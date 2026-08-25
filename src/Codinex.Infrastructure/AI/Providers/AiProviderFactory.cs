using System;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Models.AI;
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

            if (IsOpenCodeFreeProvider(provider))
                return ActivatorUtilities.CreateInstance<OpenCodeFreeProvider>(serviceProvider);

            if (IsAnthropicCompatibleProvider(provider))
                return ActivatorUtilities.CreateInstance<AnthropicCompatibleProvider>(serviceProvider);

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

        private static bool IsAnthropicCompatibleProvider(AiProvider provider)
        {
            return string.Equals(provider.Id, "anthropic", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(provider.Protocol, "anthropic", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOpenCodeFreeProvider(AiProvider provider)
        {
            return string.Equals(provider.Protocol, "opendcodefree", StringComparison.OrdinalIgnoreCase);
        }
    }
}
