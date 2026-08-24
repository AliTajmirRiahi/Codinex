using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    public interface IAiProvider
    {
        /// <summary>
        /// Sends a prompt along with multiple attachments (code, files, images) to the AI.
        /// </summary>
        Task<string> SendAsync(
            IReadOnlyList<ChatMessage> prompt,
            string chatId = null,
            string chatMessageId = null,
            CancellationToken ct = default);


        /// <summary>
        /// Sends a prompt in streaming mode and reports chunks as they arrive.
        /// </summary>
        IAsyncEnumerable<ConversationEvent> SendStreamAsync(
            IReadOnlyList<ChatMessage> messages,
            string chatId = null,
            string chatMessageId = null,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<ConversationEvent> ContinueAsync(
            IReadOnlyList<ChatMessage> history,
            string chatId = null,
            string chatMessageId = null,
            CancellationToken cancellationToken = default);
    }
}