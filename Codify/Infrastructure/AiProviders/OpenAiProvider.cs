using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Abstractions;
using Codify.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codify.Infrastructure.AiProviders
{
    /// <summary>
    /// OpenAI provider implementation using HttpClient for maximum compatibility.
    /// Supports OpenAI, local LLMs (Ollama, LM Studio), and any OpenAI-compatible API.
    /// </summary>
    public class OpenAiProvider : IAiProvider
    {
        public Task<string> SendAsync(ChatMessage prompt, IEnumerable<Attachment> attachments = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
