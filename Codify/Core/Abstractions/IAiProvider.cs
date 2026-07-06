using Codify.Core.Models;
using Codify.Storage.Models;
using Newtonsoft.Json.Linq;
using System;
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
        IReadOnlyList<ChatMessage> prompt,
        CancellationToken ct = default);


    /// <summary>
    /// Sends a prompt in streaming mode and reports chunks as they arrive.
    /// </summary>
    Task SendStreamAsync(
        IReadOnlyList<ChatMessage> prompt,
        Func<string, Task> onChunk,
        CancellationToken ct = default);
}