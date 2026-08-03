using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Core.Chat
{
    [AutoDiRegister(Modules.Chat, RegistrationOrder.Platform)]
    public sealed class ChatMessageBuilder(IReferenceContextFormatter referenceContextFormatter) : IChatMessageBuilder
    {
        private readonly IReferenceContextFormatter _referenceContextFormatter = referenceContextFormatter ?? throw new ArgumentNullException(nameof(referenceContextFormatter));

        public ChatMessageBuildResult Build(ChatMessageBuildRequest request, PromptContext promptContext)
        {
            if(request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            var messages = new List<ChatMessage>
            {
                CreateMessage("system", SystemPrompts.DeveloperOnlyAssistant)
            };

            if (!string.IsNullOrWhiteSpace(request.ProjectInstruction))
            {
                messages.Add(CreateMessage("system", BuildProjectInstruction(request.ProjectInstruction)));
            }

            if (request.SelectedAgent is not null)
            {
                messages.Add(CreateMessage("system", BuildAgentInstruction(request.SelectedAgent)));
            }

            if (request.ConversationHistory.Count > 0)
            {
                messages.AddRange(request.ConversationHistory);
            }

            var userMessage = CreateMessage("user", BuildUserContent(request, promptContext));
            AddImageData(userMessage, request.SelectedReferences);

            messages.Add(userMessage);

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

        private static string BuildProjectInstruction(string instruction)
        {
            return $"Project Instruction:\r\n{instruction.Trim()}";
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

        private static void AddImageData(ChatMessage message, IReadOnlyList<ReferenceItem> references)
        {
            var images = references?
                .Where(x => x.Type == ReferenceKind.Image && !string.IsNullOrWhiteSpace(x.Metadata?.Content))
                .Select(x => new JObject
                {
                    ["name"] = x.Name,
                    ["mimeType"] = string.IsNullOrWhiteSpace(x.Metadata.Signature) ? "image/png" : x.Metadata.Signature,
                    ["base64"] = x.Metadata.Content
                })
                .ToArray();

            if (images is not { Length: > 0 }) return;

            message.Data = new JObject
            {
                ["images"] = new JArray(images)
            };
        }

        private string BuildUserContent(ChatMessageBuildRequest request, PromptContext promptContext)
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

            if (promptContext is not null && promptContext.Sections.Count > 0)
            {
                sb.AppendLine("Workspace Context:");
                sb.AppendLine();

                foreach (var section in promptContext.Sections)
                {
                    sb.AppendLine($"## {section.Name}");

                    foreach (var item in section.Items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Title))
                        {
                            sb.AppendLine($"### {item.Title}");
                        }

                        sb.AppendLine(item.Content);
                        sb.AppendLine();
                    }
                }
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
