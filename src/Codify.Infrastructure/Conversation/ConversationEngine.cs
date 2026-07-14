using Codify.Core.Conversation;
using Codify.Core.Models;
using Codify.Storage.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;

namespace Codify.Infrastructure.Conversation
{
    public sealed class ConversationEngine(
        IChatMessageBuilder chatMessageBuilder,
        IAiProvider provider)
        : IConversationEngine
    {
        public async IAsyncEnumerable<ConversationEvent> ExecuteAsync(
            ChatMessageBuildRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return ConversationEvent.Status("Building prompt...");

            var buildResult = chatMessageBuilder.Build(request);

            yield return ConversationEvent.Status("Sending request...");

            await foreach (var evt in provider.SendStreamAsync(
                               buildResult.Messages,
                               cancellationToken))
            {
                yield return evt;
            }

            yield return ConversationEvent.Completed();
        }
    }
}
