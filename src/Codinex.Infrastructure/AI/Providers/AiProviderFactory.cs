using System;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Codinex.Infrastructure.AI.Providers
{
    /// <summary>
    /// Creates fresh AI provider instances using dependency injection for construction only.
    /// </summary>
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public class AiProviderFactory(IServiceProvider serviceProvider) : IAiProviderFactory
    {
        public IAiProvider Create(AiProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (provider.IsLocal)
                return ActivatorUtilities.CreateInstance<OllamaProvider>(serviceProvider);

            if (IsOpenAiCompatibleProvider(provider))
                return ActivatorUtilities.CreateInstance<OpenAiCompatibleProvider>(serviceProvider);

            throw new NotSupportedException($"Provider {provider.Name} is not supported.");
        }


        private static bool IsOpenAiCompatibleProvider(AiProvider provider)
        {
            return string.Equals(provider.Id, "gapgpt", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(provider.Id, "openai", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(provider.Protocol, "openai", StringComparison.OrdinalIgnoreCase);
        }
    }
}
