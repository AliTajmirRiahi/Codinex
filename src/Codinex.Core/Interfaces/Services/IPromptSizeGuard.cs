using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Core.Interfaces.Services
{
    /// <summary>
    /// Gates each outgoing model request on its serialized payload size. When the payload is
    /// large enough to matter for token cost, it shows an in-chat warning and blocks the send
    /// until the user chooses Continue or Stop. After a Continue the effective threshold for
    /// that chat is raised by one base step, so the user is not asked again on the very next
    /// request - only once the payload grows past the new, higher limit.
    /// </summary>
    public interface IPromptSizeGuard
    {
        /// <summary>
        /// Returns immediately with <c>true</c> when the warning is disabled or the payload is
        /// under the (possibly escalated) threshold for <paramref name="chatId"/>. Otherwise
        /// posts the warning to the chat webview and waits: <c>true</c> if the user chose
        /// Continue, <c>false</c> if the user chose Stop or the wait was cancelled.
        /// </summary>
        Task<bool> ConfirmAsync(int payloadByteCount, string chatId, CancellationToken cancellationToken);

        /// <summary>
        /// Resolves the matching <see cref="ConfirmAsync"/> call for this request id when the
        /// webview posts the user's decision back. Returns false when nothing is waiting.
        /// </summary>
        bool SubmitDecision(string requestId, bool proceed);

        /// <summary>
        /// Clears the per-chat threshold escalation. Called when the base setting changes so a
        /// lowered limit takes effect immediately instead of being masked by past Continues.
        /// </summary>
        void ResetEscalation();
    }
}
