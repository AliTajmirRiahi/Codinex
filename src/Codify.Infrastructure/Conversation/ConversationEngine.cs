using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.Infrastructure.Tools;
using Codify.Storage.Models;
using System;
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

            await foreach (var evt in provider.SendStreamAsync(
                               request.Messages,
                               cancellationToken))
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

                        await foreach (var evt2 in provider.ContinueAsync(
                                           [result],
                                           cancellationToken))
                        {
                            yield return evt2;
                        }

                        break;

                    default:

                        yield return evt;

                        break;
                }
            }

            yield return ConversationEvent.Completed();
        }
    }
}
