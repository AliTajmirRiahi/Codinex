using Codify.Core.Conversation;
using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Core.Interfaces
{
    public interface IAiProvider
    {
        /// <summary>
        /// Sends a prompt along with multiple attachments (code, files, images) to the AI.
        /// </summary>
        Task<string> SendAsync(
            IReadOnlyList<ChatMessage> prompt,
            CancellationToken ct = default);


        /// <summary>
        /// Sends a prompt in streaming mode and reports chunks as they arrive.
        /// </summary>
        IAsyncEnumerable<ConversationEvent> SendStreamAsync(
            IReadOnlyList<ChatMessage> messages,
            CancellationToken cancellationToken = default);
    }
}