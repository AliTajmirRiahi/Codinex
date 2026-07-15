using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;

namespace Codify.Infrastructure.ModelManagement.Retrievers
{
    public class OpenAiCompatibleModelRetriever : IModelRetriever
    {
        public bool CanHandle(AiProvider provider)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<AiModel>> GetModelsAsync(AiProvider provider, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}