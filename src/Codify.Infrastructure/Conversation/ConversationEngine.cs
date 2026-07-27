using Codify.Core.Conversation;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Tools;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.Conversation
{
    [AutoDiRegister(Modules.Conversation, RegistrationOrder.Infrastructure)]
    public sealed class ConversationEngine(
        IChatMessageBuilder chatMessageBuilder,
        IAiProvider provider,
        IAiToolRegistry toolRegistry)
        : IConversationEngine
    {
        public async IAsyncEnumerable<ConversationEvent> ExecuteAsync(
            ChatMessageBuildResult request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return ConversationEvent.Status("Sending request...");

            await foreach (var evt in ProcessEvents(
                               request.Messages,
                               provider.SendStreamAsync(
                                   request.Messages,
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
            IReadOnlyList<ChatMessage> messages,
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

                            yield return ConversationEvent.ToolCompleted(result);
                        }

                        await foreach (var continuationEvent in ProcessEvents(
                                           messages,
                                           provider.ContinueAsync(
                                               messages,
                                               assistantMessage,
                                               results,
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