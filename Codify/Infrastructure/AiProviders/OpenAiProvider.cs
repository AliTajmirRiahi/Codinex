using Codify.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Infrastructure.AiProviders
{
    public class OpenAiProvider : IAiProvider
    {
        public Task<string> SendAsync(string prompt, IEnumerable<Attachment> attachments = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public string Name { get; }
        public bool SupportsImages { get; }
    }
}
