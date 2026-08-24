using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;

namespace Codinex.Infrastructure.ModelManagement
{
    [AutoDiRegister(Modules.ModelManagement, RegistrationOrder.Infrastructure)]
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

            if (provider.NeedApiKey && string.IsNullOrWhiteSpace(provider.ApiKey))
                return localModels;

            var onlineModels = await GetModelsFromServerAsync(provider, cancellationToken);

            return onlineModels.Any()
                ? onlineModels.ToList()
                : localModels;
        }

        public async Task<List<AiModel>> GetModelsFromServerAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            var retriever = retrievers.FirstOrDefault(x => x.CanHandle(provider));

            if (retriever == null)
                return [];

            try
            {
                var onlineModels = await retriever.GetModelsAsync(provider, cancellationToken);

                return onlineModels.ToList();
            }
            catch
            {
                return [];
            }    
        }

    }
}