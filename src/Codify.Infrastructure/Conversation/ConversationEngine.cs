using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Tools;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.Conversation
{
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
            IAsyncEnumerable<ConversationEvent> events,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var evt in events.WithCancellation(cancellationToken))
            {
                switch (evt.Type)
                {
                    case ConversationEventType.ToolRequested:

                        var toolRequest = evt.Payload.ToObject<ToolRequest>();

                        var tool = toolRegistry.Get(toolRequest.Name);

                        yield return ConversationEvent.Status(
                            $"Executing tool '{tool.Name}'...");

                        var result = await tool.ExecuteAsync(
                            toolRequest,
                            cancellationToken);

                        yield return ConversationEvent.ToolCompleted(result);

                        await foreach (var continuationEvent in ProcessEvents(
                                           provider.ContinueAsync(
                                               new[] { result },
                                               cancellationToken),
                                           cancellationToken))
                        {
                            yield return continuationEvent;
                        }

                        break;

                    default:

                        yield return evt;

                        break;
                }
            }
        }
    }
}