using Codify.Core.Interfaces;
using Codify.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.ModelManagement
{
    public class ProviderModelService(
        IModelResourceLoader resourceLoader,
        IEnumerable<IModelRetriever> retrievers)
        : IProviderModelService
    {
        public async Task<List<AiModel>> GetModelsAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            // Always load local models first.
            var localModels = await resourceLoader.LoadAsync(provider, cancellationToken);

            if (string.IsNullOrWhiteSpace(provider.ApiKey))
                return localModels;

            var retriever = retrievers.FirstOrDefault(x => x.CanHandle(provider));

            if (retriever == null)
                return localModels;

            var onlineModels = await retriever.GetModelsAsync(provider, cancellationToken);

            return onlineModels.Any()
                ? onlineModels.ToList()
                : localModels;
        }
    }
}