using Codify.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Infrastructure.AiProviders
{
    public class GapGptProvider : IAiProvider
    {
        public string Name { get; }
        public bool SupportsImages { get; }

        public async Task<string> SendAsync(string prompt, IEnumerable<Attachment> attachments = null, CancellationToken ct = default)
        {
            await Task.Delay(800, ct);
            return "Gotcha";
        }

    }
}
