using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Storage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AiProviders
{
    /// <summary>
    /// Routes requests to the correct provider based on model family.
    /// </summary>
    public class AiProviderRouter : IAiRouterProvider
    {
        public IEnumerable<AiProviderInfo> Providers { get; }

        public AiProviderRouter(IEnumerable<AiProviderInfo> providers)
        {
            Providers = providers;
        }

        public AiProvider Provider => new AiProvider("router","router","");
        public AiModel Model => throw new NotSupportedException("Router does not have a specific model.");


        public async Task<string> SendAsync(
            ChatMessage prompt,
            IEnumerable<Attachment> attachments = null,
            CancellationToken ct = default)
        {
            if (prompt == null && prompt?.Family == AiProviderFamily.NaN)
                throw new ArgumentException(@"Prompt must specify a valid model family.", nameof(prompt));

            var family = prompt?.Family;

            var provider = Providers
                .FirstOrDefault(p => p.Family == family);

            if (provider == null)
                throw new NotSupportedException(
                    $"No provider found for family {family}");

            return await provider.AiProvider.SendAsync(prompt, attachments, ct);
        }
    }
}