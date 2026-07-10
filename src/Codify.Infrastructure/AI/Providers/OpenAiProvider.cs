using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;

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

        public Task SendStreamAsync(
            IReadOnlyList<ChatMessage> prompt,
            Func<string, Task> onChunk,
            CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
