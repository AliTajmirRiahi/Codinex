using Codify.Core.Abstractions;
using Codify.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AiProviders
{
    public class GapGptProvider : IAiProvider
    {
        public string Name { get; }
        public bool SupportsImages { get; }

        public async Task<string> SendAsync(ChatMessage prompt, IEnumerable<Attachment> attachments = null, CancellationToken ct = default)
        {
            await Task.Delay(800, ct);
            return "Gotcha";
        }

    }
}
