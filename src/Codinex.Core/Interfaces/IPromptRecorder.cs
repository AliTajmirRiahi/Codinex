using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Records the raw outgoing AI request payload for a chat turn, so it can later be
    /// attached to bug reports.
    /// </summary>
    public interface IPromptRecorder
    {
        Task RecordAsync(
            string chatId,
            string chatMessageId,
            string payloadContent,
            CancellationToken cancellationToken = default);
    }
}
