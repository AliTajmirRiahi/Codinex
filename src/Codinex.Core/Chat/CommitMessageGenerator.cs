using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;

namespace Codinex.Core.Chat
{
    /// <summary>
    /// Generates a commit message from the current Git changes using the active AI provider.
    /// </summary>
    [AutoDiRegister(Modules.Chat, RegistrationOrder.Features)]
    public sealed class CommitMessageGenerator(
        IGitContextProvider gitContextProvider,
        IAiProviderRouter aiProviderRouter)
        : ICommitMessageGenerator
    {
        public async Task<string> GenerateAsync(string CommitMessageSystemPrompt, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = await gitContextProvider.GetContextAsync(cancellationToken);

            var files = SelectDiffSource(context.Files);

            if (files.Count == 0)
            {
                throw new NoGitChangesException();
            }

            var diffText = BuildDiffText(files);

            var messages = new List<ChatMessage>
            {
                new()
                {
                    Role = "system",
                    Content = "YOU WRITE GIT COMMIT MESSAGES FROM A UNIFIED DIFF.\n" + CommitMessageSystemPrompt
                },
                new()
                {
                    Role = "user",
                    Content = $"Generate a commit message for the following changes:\n\n{diffText}"
                }
            };

            var provider = aiProviderRouter.GetCurrentProvider();

            // SendAsync() silently swallows provider errors (network/auth/quota) and returns
            // the error text as if it were a normal response, so it can be shown as a chat
            // bubble. That would get written into the commit box as a fake commit message.
            // SendStreamAsync() instead reports them as a distinct ConversationFailed event,
            // so we can tell a real error apart from real generated content.
            var builder = new StringBuilder();

            await foreach (var conversationEvent in provider.SendStreamAsync(messages, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (conversationEvent.Type)
                {
                    case ConversationEventType.TextDelta:
                        builder.Append(conversationEvent.Payload?.ToString());
                        break;
                    case ConversationEventType.ConversationFailed:
                        throw new CommitMessageProviderException(
                            string.IsNullOrWhiteSpace(conversationEvent.DisplayMessage)
                                ? "The AI provider returned an error."
                                : conversationEvent.DisplayMessage);
                    case ConversationEventType.ConversationCancelled:
                        throw new OperationCanceledException(cancellationToken);
                    case ConversationEventType.ConversationCompleted:
                        return CleanUp(builder.ToString());
                }
            }

            return CleanUp(builder.ToString());
        }

        private static IReadOnlyList<GitFileItem> SelectDiffSource(IReadOnlyList<GitFileItem> files)
        {
            if (files == null)
            {
                return Array.Empty<GitFileItem>();
            }

            var staged = files.Where(x => x.IsStaged).ToList();

            return staged.Count > 0 ? staged : files;
        }

        private static string BuildDiffText(IReadOnlyList<GitFileItem> files)
        {
            var builder = new StringBuilder();

            foreach (var file in files)
            {
                builder.AppendLine($"## {file.Path} ({file.Status}, +{file.LinesAdded} -{file.LinesDeleted})");
                builder.AppendLine();

                if (!string.IsNullOrEmpty(file.Diff))
                {
                    builder.AppendLine(file.Diff);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string CleanUp(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            var trimmed = message.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewLine = trimmed.IndexOf('\n');
                if (firstNewLine >= 0)
                {
                    trimmed = trimmed.Substring(firstNewLine + 1);
                }

                var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0)
                {
                    trimmed = trimmed.Substring(0, lastFence);
                }

                trimmed = trimmed.Trim();
            }

            return NormalizeBulletStructure(trimmed) + '\n';
        }

        /// <summary>
        /// Some models don't reliably emit real newlines for the bullet list — they run the
        /// summary and every bullet together separated by " - " instead. If that happens,
        /// reconstruct the intended structure: summary line, blank line, one indented bullet
        /// per line. Messages that already contain real newlines are left untouched.
        /// </summary>
        private static string NormalizeBulletStructure(string message)
        {
            if (string.IsNullOrEmpty(message) || message.Contains("\n"))
            {
                return message;
            }

            var segments = message.Split([" - "], StringSplitOptions.None);

            if (segments.Length <= 1)
            {
                return message;
            }

            var builder = new StringBuilder();
            builder.Append(segments[0].TrimEnd());
            builder.AppendLine();
            builder.AppendLine();

            for (var i = 1; i < segments.Length; i++)
            {
                var bullet = segments[i].Trim();

                if (bullet.Length == 0)
                {
                    continue;
                }

                builder.Append("       - ").Append(bullet);

                if (i < segments.Length - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }
    }
}
