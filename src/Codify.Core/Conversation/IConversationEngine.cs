using System.Collections.Generic;
using System.Threading;
using Codify.Core.Models;

namespace Codify.Core.Conversation
{
    public interface IConversationEngine
    {
        IAsyncEnumerable<ConversationEvent> ExecuteAsync(
            ChatMessageBuildRequest request,
            CancellationToken cancellationToken = default);
    }
}
