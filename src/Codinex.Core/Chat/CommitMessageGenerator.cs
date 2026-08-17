using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task<string> GenerateAsync(CancellationToken cancellationToken)
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
                    Content = SystemPrompts.CommitMessageSystemPrompt
                },
                new()
                {
                    Role = "user",
                    Content = $"Generate a commit message for the following changes:\n\n{diffText}"
                }
            };

            var provider = aiProviderRouter.GetCurrentProvider();

            var response = await provider.SendAsync(messages, cancellationToken);

            return CleanUp(response);
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

            return trimmed;
        }
    }
}
