using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;

namespace Codify.Infrastructure.ModelManagement
{
    public class ProviderModelService(IEnumerable<IModelRetriever> retrievers) : IProviderModelService
    {
        private readonly IEnumerable<IModelRetriever> _retrievers = retrievers;

        public Task<List<AiModel>> GetModelsAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}