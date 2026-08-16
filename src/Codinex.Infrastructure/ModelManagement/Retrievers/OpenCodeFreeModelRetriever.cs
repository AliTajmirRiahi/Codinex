using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.AI.Providers.OpenCode;

namespace Codinex.Infrastructure.ModelManagement.Retrievers
{
    /// <summary>
    /// Discovers OpenCode's free models dynamically from "GET /provider" instead of hardcoding
    /// model ids, since OpenCode may add/remove/rename its free models at any time. A model is
    /// considered free when its "opencode" provider entry has cost.input == 0 and cost.output == 0.
    /// </summary>
    [AutoDiRegister(Modules.ModelManagement, RegistrationOrder.Infrastructure)]
    public class OpenCodeFreeModelRetriever(
        IProviderClient client,
        IJsonSerializer jsonSerializer) : IModelRetriever
    {
        private const string Protocol = "opendcodefree";

        public bool CanHandle(AiProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return string.Equals(provider.Protocol, Protocol, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlyList<AiModel>> GetModelsAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            var response = await client.GetAsync(
                provider,
                provider.ModelEndPoint,
                cancellationToken);

            var payload = jsonSerializer.Deserialize<OpenCodeProviderListResponseDto>(response);

            var upstreamProvider = OpenCodeCatalog.FindUpstreamProvider(payload);

            if (upstreamProvider?.Models == null)
                return [];

            return upstreamProvider.Models
                .Where(kvp => IsFree(kvp.Value))
                .Select(kvp => CreateModel(kvp.Key, kvp.Value))
                .ToList();
        }

        private static bool IsFree(OpenCodeModelDto model)
        {
            return model?.Cost != null
                   && model.Cost.Input == 0
                   && model.Cost.Output == 0;
        }

        private static AiModel CreateModel(string key, OpenCodeModelDto model)
        {
            var id = !string.IsNullOrWhiteSpace(model.Id) ? model.Id : key;

            var aiModel = AiModel.CreateRemote(id);

            aiModel.Family = AiProviderFamily.Custom;

            if (!string.IsNullOrWhiteSpace(model.Name))
                aiModel.Rename(model.Name);

            var contextLimit = model.Limit?.Context ?? 0;

            if (contextLimit > 0)
                aiModel.UpdateTokenLimit(contextLimit);

            return aiModel;
        }
    }
}
