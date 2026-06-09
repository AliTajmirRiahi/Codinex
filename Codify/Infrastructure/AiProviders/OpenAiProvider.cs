using Codify.Core.Abstractions;
using Codify.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AiProviders
{
    public class OpenAiProvider : IAiProvider
    {
        public Task<string> SendAsync(ChatMessage prompt, IEnumerable<Attachment> attachments = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public string Name { get; }
        public bool SupportsImages { get; }
    }
}
