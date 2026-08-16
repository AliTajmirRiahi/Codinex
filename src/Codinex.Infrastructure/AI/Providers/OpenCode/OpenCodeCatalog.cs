using System;
using System.Linq;

namespace Codinex.Infrastructure.AI.Providers.OpenCode
{
    /// <summary>
    /// Shared lookups over the "GET /provider" response, used by both model discovery
    /// (<see cref="Codinex.Infrastructure.ModelManagement.Retrievers.OpenCodeFreeModelRetriever"/>)
    /// and capability checking (<see cref="Codinex.Infrastructure.AI.Capabilities.ProviderCapabilityChecker"/>).
    /// </summary>
    internal static class OpenCodeCatalog
    {
        public const string UpstreamProviderId = "opencode";

        public static OpenCodeProviderDto FindUpstreamProvider(OpenCodeProviderListResponseDto payload)
        {
            return payload?.All?.FirstOrDefault(p =>
                string.Equals(p.Id, UpstreamProviderId, StringComparison.OrdinalIgnoreCase));
        }

        public static OpenCodeModelDto FindModel(OpenCodeProviderListResponseDto payload, string modelId)
        {
            var provider = FindUpstreamProvider(payload);

            if (provider?.Models == null || string.IsNullOrWhiteSpace(modelId))
                return null;

            foreach (var kvp in provider.Models)
            {
                var id = !string.IsNullOrWhiteSpace(kvp.Value?.Id) ? kvp.Value.Id : kvp.Key;

                if (string.Equals(id, modelId, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }
    }
}
