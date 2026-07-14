using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AI.Providers
{
    /// <summary>
    /// OpenAI provider implementation using HttpClient for maximum compatibility.
    /// Supports OpenAI, local LLMs (Ollama, LM Studio), and any OpenAI-compatible API.
    /// </summary>
    public class OpenAiProvider : IAiProvider
    {
        public Task<string> SendAsync(IReadOnlyList<ChatMessage> prompt, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<ConversationEvent> SendStreamAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<ConversationEvent> ContinueAsync(
            IReadOnlyList<ToolResult> toolResults,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var toolResult in toolResults)
            {
                yield return ConversationEvent.Status(
                    $"Tool '{toolResult.Id}' completed.");
            }

            yield return ConversationEvent.TextDelta(
                "Conversation continuation is not implemented yet.");

            yield return ConversationEvent.Completed();
        }
    }
}
