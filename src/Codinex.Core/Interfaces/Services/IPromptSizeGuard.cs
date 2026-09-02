using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Core.Interfaces.Services
{
    /// <summary>
    /// Gates each outgoing model request on its serialized payload size. When the payload is
    /// large enough to matter for token cost, it shows an in-chat warning and blocks the send
    /// until the user chooses Continue or Stop.
    /// </summary>
    public interface IPromptSizeGuard
    {
        /// <summary>
        /// Returns immediately with <c>true</c> when the warning is disabled or the payload is
        /// under the configured threshold. Otherwise posts the warning to the chat webview and
        /// waits: <c>true</c> if the user chose Continue, <c>false</c> if the user chose Stop or
        /// the wait was cancelled.
        /// </summary>
        Task<bool> ConfirmAsync(int payloadByteCount, CancellationToken cancellationToken);

        /// <summary>
        /// Resolves the matching <see cref="ConfirmAsync"/> call for this request id when the
        /// webview posts the user's decision back. Returns false when nothing is waiting.
        /// </summary>
        bool SubmitDecision(string requestId, bool proceed);
    }
}
