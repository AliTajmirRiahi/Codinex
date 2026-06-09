using Codify.Core.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Core.Abstractions;

public interface IAiProvider
{
    /// <summary>
    /// Sends a prompt along with multiple attachments (code, files, images) to the AI.
    /// </summary>
    Task<string> SendAsync(
        ChatMessage prompt,
        IEnumerable<Attachment> attachments = null,
        CancellationToken ct = default);

    /// <summary>
    /// Metadata about the provider (e.g., "Ollama - Llama 3", "OpenAI GPT-4o")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Supported capabilities (can it see images? can it handle large code?)
    /// </summary>
    bool SupportsImages { get; }
}