using System;
using System.Collections.Generic;
using System.Text;
using Codify.Core.Interfaces;
using Codify.Core.Models;

namespace Codify.Core.Chat
{
    public sealed class ChatMessageBuilder(IReferenceContextFormatter referenceContextFormatter) : IChatMessageBuilder
    {
        private readonly IReferenceContextFormatter _referenceContextFormatter = referenceContextFormatter ?? throw new ArgumentNullException(nameof(referenceContextFormatter));

        public ChatMessageBuildResult Build(ChatMessageBuildRequest request)
        {
            if(request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            var messages = new List<ChatMessage>
            {
                CreateMessage("system", SystemPrompts.DeveloperOnlyAssistant)
            };

            if (request.SelectedAgent is not null)
            {
                messages.Add(CreateMessage("system", BuildAgentInstruction(request.SelectedAgent)));
            }

            if (request.ConversationHistory.Count > 0)
            {
                messages.AddRange(request.ConversationHistory);
            }

            messages.Add(CreateMessage("user", BuildUserContent(request)));

            return new ChatMessageBuildResult
            {
                Messages = messages,
                Context = new ChatMessageRequestContext
                {
                    SelectedCommand = request.SelectedCommand,
                    SelectedAgent = request.SelectedAgent,
                    SelectedReferences = request.SelectedReferences
                }
            };
        }

        private static ChatMessage CreateMessage(string role, string content)
        {
            return new ChatMessage
            {
                Role = role,
                Content = content
            };
        }

        private static string BuildAgentInstruction(ChatAgent agent)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Agent: {agent.Name}");

            if (!string.IsNullOrWhiteSpace(agent.Description))
            {
                sb.AppendLine($"Agent Description: {agent.Description}");
            }

            return sb.ToString().TrimEnd();
        }

        private string BuildUserContent(ChatMessageBuildRequest request)
        {
            var sb = new StringBuilder();

            if (request.SelectedCommand is not null)
            {
                sb.AppendLine($"Command: {request.SelectedCommand.Name}");

                if (!string.IsNullOrWhiteSpace(request.SelectedCommand.Description))
                {
                    sb.AppendLine($"Command Description: {request.SelectedCommand.Description}");
                }

                sb.AppendLine();
            }

            if (request.SelectedReferences.Count > 0)
            {
                sb.AppendLine("Selected References:");

                foreach (var reference in request.SelectedReferences)
                {
                    sb.AppendLine(_referenceContextFormatter.Format(reference));
                    sb.AppendLine();
                }
            }

            sb.AppendLine("User Request:");
            sb.AppendLine(request.DraftText);

            return sb.ToString().TrimEnd();
        }
    }
}
