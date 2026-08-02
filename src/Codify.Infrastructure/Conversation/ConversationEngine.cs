using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Models.Tools;
using Codify.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.Conversation
{
    [AutoDiRegister(Modules.Conversation, RegistrationOrder.Infrastructure)]
    public sealed class ConversationEngine(
        IChatMessageBuilder chatMessageBuilder,
        IAiProvider provider,
        IAiToolRegistry toolRegistry,
        IJsonSerializer jsonSerializer)
        : IConversationEngine
    {
        public async IAsyncEnumerable<ConversationEvent> ExecuteAsync(
            ChatMessageBuildResult request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return ConversationEvent.Status("Sending request...");

            var history = request.Messages.ToList();

            await foreach (var evt in ProcessEvents(
                               history,
                               provider.SendStreamAsync(
                                   history,
                                   cancellationToken),
                               cancellationToken))
            {
                yield return evt;
            }
        }

        /// <summary>
        /// Processes conversation events recursively.
        /// </summary>
        private async IAsyncEnumerable<ConversationEvent> ProcessEvents(
            List<ChatMessage> history,
            IAsyncEnumerable<ConversationEvent> events,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var evt in events.WithCancellation(cancellationToken))
            {
                switch (evt.Type)
                {
                    case ConversationEventType.ToolRequested:

                        var payload = evt.Payload.ToObject<ToolRequestedPayload>();

                        var assistantMessage = payload.AssistantMessage;

                        history.Add(assistantMessage);

                        var results = new List<ToolResult>();

                        foreach (var toolRequest in payload.Requests)
                        {
                            var tool = toolRegistry.Get(toolRequest.Name);

                            var statusMessage = GetStatusMessage(tool, toolRequest);

                            if (!string.IsNullOrWhiteSpace(statusMessage))
                            {
                                yield return ConversationEvent.Status(statusMessage);
                            }

                            var result = await tool.ExecuteAsync(
                                toolRequest,
                                cancellationToken);

                            results.Add(result);

                            history.Add(new ChatMessage
                            {
                                Role = "tool",
                                ToolCallId = result.Id,
                                Content = jsonSerializer.Serialize(result.Data),
                            });

                            yield return ConversationEvent.ToolCompleted(result);
                        }

                        await foreach (var continuationEvent in ProcessEvents(
                                           history,
                                           provider.ContinueAsync(
                                               history,
                                               cancellationToken),
                                           cancellationToken))
                        {
                            yield return continuationEvent;
                        }

                        break;

                    case ConversationEventType.Unknown:
                    case ConversationEventType.TextDelta:
                    case ConversationEventType.ThinkingStarted:
                    case ConversationEventType.ThinkingUpdated:
                    case ConversationEventType.ThinkingCompleted:
                    case ConversationEventType.ToolCompleted:
                    case ConversationEventType.ConversationCompleted:
                    case ConversationEventType.ConversationCancelled:
                    case ConversationEventType.ConversationFailed:
                    case ConversationEventType.StatusChanged:
                    default:

                        yield return evt;

                        break;
                }
            }
        }

        private static string GetStatusMessage(
            IAiTool tool,
            ToolRequest request)
        {
            var statusMessage = tool.StatusMessage;

            if (string.IsNullOrWhiteSpace(statusMessage))
            {
                return statusMessage;
            }

            var detail = GetStatusDetail(request);

            if (string.IsNullOrWhiteSpace(detail))
            {
                return statusMessage;
            }

            return $"{statusMessage} ({detail})";
        }

        private static string GetStatusDetail(ToolRequest request)
        {
            if (request.Arguments is not { HasValues: true })
            {
                return string.Empty;
            }

            foreach (var name in new[] { "path", "query", "symbol", "id", "title" })
            {
                var detail = request.Arguments.Value<string>(name);

                if (string.IsNullOrWhiteSpace(detail))
                {
                    continue;
                }

                return name == "path" ? GetPathDisplayName(detail) : detail;
            }

            return string.Empty;
        }

        private static string GetPathDisplayName(string path)
        {
            var normalizedPath = path
                .Replace('\\', '/')
                .TrimEnd('/');

            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return string.Empty;
            }

            var lastSeparatorIndex = normalizedPath.LastIndexOf('/');

            return lastSeparatorIndex >= 0
                ? normalizedPath.Substring(lastSeparatorIndex + 1)
                : normalizedPath;
        }
    }
}