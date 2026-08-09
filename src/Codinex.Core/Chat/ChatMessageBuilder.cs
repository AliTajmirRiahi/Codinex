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
    public sealed class ChatMessageBuilder(
        IReferenceContextFormatter referenceContextFormatter,
        IPromptProfiler promptProfiler) : IChatMessageBuilder
    {
        private readonly IReferenceContextFormatter _referenceContextFormatter = referenceContextFormatter ?? throw new ArgumentNullException(nameof(referenceContextFormatter));
        private readonly IPromptProfiler _promptProfiler = promptProfiler ?? throw new ArgumentNullException(nameof(promptProfiler));

        public ChatMessageBuildResult Build(ChatMessageBuildRequest request, PromptContext promptContext)
        {
            if(request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            var messages = new List<ChatMessage>
            {
                CreateMessage("system", SystemPrompts.DeveloperOnlyAssistant)
            };

            if (!string.IsNullOrWhiteSpace(request.ProjectName) ||
                !string.IsNullOrWhiteSpace(request.ProjectInstruction))
            {
                messages.Add(CreateMessage("system", BuildProjectInstruction(request.ProjectName, request.ProjectInstruction)));
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

            var requestContext = new ChatMessageRequestContext
            {
                SelectedCommand = request.SelectedCommand,
                SelectedAgent = request.SelectedAgent,
                SelectedReferences = request.SelectedReferences,
                PromptProfile = _promptProfiler.Profile(BuildPromptProfileContext(request, promptContext, messages))
            };

            userMessage.Context = requestContext;

            return new ChatMessageBuildResult
            {
                Messages = messages,
                Context = requestContext
            };
        }

        private static ChatMessage CreateMessage(string role, string content)
        {
            return new ChatMessage
            {
                Role = role,
                Content = content,
            };
        }

        private static string BuildProjectInstruction(string projectName, string instruction)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Project Instruction:");

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                sb.AppendLine($"Project Name: {projectName.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(instruction))
            {
                sb.AppendLine(instruction.Trim());
            }

            return sb.ToString().TrimEnd();
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

        private PromptContext BuildPromptProfileContext(
            ChatMessageBuildRequest request,
            PromptContext promptContext,
            IReadOnlyList<ChatMessage> messages)
        {
            var context = new PromptContext();

            AddProfileSection(
                context,
                "System Prompt",
                string.Join(Environment.NewLine, messages
                    .Where(x => string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Content)
                    .Where(x => !string.IsNullOrWhiteSpace(x))));

            AddWorkspaceProfileSection(context, promptContext, request);

            AddProfileSection(
                context,
                "Conversation",
                BuildConversationProfileContent(request));

            return context;
        }

        private static void AddProfileSection(PromptContext context, string name, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            context.Sections.Add(new PromptContextSection
            {
                Name = name,
                Items = new List<PromptContextItem>
                {
                    new()
                    {
                        Content = content
                    }
                }
            });
        }

        private void AddWorkspaceProfileSection(
            PromptContext context,
            PromptContext promptContext,
            ChatMessageBuildRequest request)
        {
            var items = new List<PromptContextItem>();

            if (promptContext?.Sections != null)
            {
                foreach (var section in promptContext.Sections)
                {
                    foreach (var item in section.Items ?? new List<PromptContextItem>())
                    {
                        items.Add(new PromptContextItem
                        {
                            Kind = item.Kind,
                            Title = string.IsNullOrWhiteSpace(item.Title) ? section.Name : item.Title,
                            Content = item.Content,
                            Reason = BuildWorkspaceProfileReason(item)
                        });
                    }
                }
            }

            var selectedReferences = BuildSelectedReferencesProfileContent(request);

            if (!string.IsNullOrWhiteSpace(selectedReferences))
            {
                items.Add(new PromptContextItem
                {
                    Title = "Selected References",
                    Content = selectedReferences,
                    Reason = BuildSelectedReferencesReason(request)
                });
            }

            if (items.Count == 0)
            {
                return;
            }

            context.Sections.Add(new PromptContextSection
            {
                Name = "Workspace",
                Items = items
            });
        }

        private static string BuildWorkspaceProfileContent(PromptContext promptContext)
        {
            if (promptContext == null || promptContext.Sections.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

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

            return sb.ToString().TrimEnd();
        }

        private static string BuildWorkspaceProfileReason(PromptContextItem item)
        {
            return item?.Kind switch
            {
                PromptContextKind.CurrentDocument => "Included by CurrentDocumentContextProvider",
                PromptContextKind.Memory => "Included by MemoryContextProvider",
                PromptContextKind.Project => "Included by ProjectContextProvider",
                PromptContextKind.Git => "Included by GitContextProvider",
                PromptContextKind.Diagnostics => "Included by DiagnosticsContextProvider",
                PromptContextKind.Tool => "Included by ToolContextProvider",
                PromptContextKind.OpenDocuments => "Included by OpenDocumentsContextProvider",
                PromptContextKind.Build => "Included by BuildContextProvider",
                _ => "Included by WorkspaceContextProvider"
            };
        }

        private static string BuildSelectedReferencesReason(ChatMessageBuildRequest request)
        {
            var count = request.SelectedReferences?.Count ?? 0;
            var label = count == 1 ? "file" : "files";

            return $"User attached {count} {label}";
        }

        private static string BuildConversationProfileContent(ChatMessageBuildRequest request)
        {
            var sb = new StringBuilder();

            foreach (var message in request.ConversationHistory ?? Array.Empty<ChatMessage>())
            {
                sb.AppendLine($"{message.Role}:");

                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    sb.AppendLine(message.Content);
                }

                if (message.ToolCalls is { Count: > 0 })
                {
                    foreach (var toolCall in message.ToolCalls)
                    {
                        sb.AppendLine($"Tool Call: {toolCall.Name}");
                        sb.AppendLine(toolCall.Arguments?.ToString() ?? string.Empty);
                    }
                }

                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.DraftText))
            {
                sb.AppendLine("user:");
                sb.AppendLine(request.DraftText);
            }

            return sb.ToString().TrimEnd();
        }

        private string BuildSelectedReferencesProfileContent(ChatMessageBuildRequest request)
        {
            if (request.SelectedReferences == null || request.SelectedReferences.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            foreach (var reference in request.SelectedReferences)
            {
                sb.AppendLine(_referenceContextFormatter.Format(reference));
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
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
