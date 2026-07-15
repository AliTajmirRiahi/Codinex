using Codify.Core.Interfaces;
using Codify.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ProviderModelService(IEnumerable<IModelRetriever> retrievers) : IProviderModelService
{
    private readonly IEnumerable<IModelRetriever> _retrievers = retrievers;

    public async Task<List<AiModel>> GetModelsAsync(AiProvider provider)
    {
        await Task.Delay(100);

        return null;
    }
}