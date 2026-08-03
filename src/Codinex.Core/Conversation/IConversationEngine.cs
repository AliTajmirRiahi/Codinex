using System.Collections.Generic;
using System.Threading;
using Codinex.Core.Models;

namespace Codinex.Core.Conversation
{
    public interface IConversationEngine
    {
        IAsyncEnumerable<ConversationEvent> ExecuteAsync(
            ChatMessageBuildResult request,
            CancellationToken cancellationToken = default);
    }
}
