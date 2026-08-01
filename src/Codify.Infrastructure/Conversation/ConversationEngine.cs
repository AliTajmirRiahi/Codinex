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

                            yield return ConversationEvent.Status(
                                $"Executing tool '{tool.Name}'...");

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
    }
}