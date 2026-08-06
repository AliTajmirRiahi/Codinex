using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Conversation
{
    public interface IConversationEngine
    {
        Task<string> ExecuteTextAsync(
            ChatMessageBuildResult request,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<ConversationEvent> ExecuteAsync(
            ChatMessageBuildResult request,
            CancellationToken cancellationToken = default);
    }
}
