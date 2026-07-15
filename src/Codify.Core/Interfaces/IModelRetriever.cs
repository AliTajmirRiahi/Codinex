using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces;

public interface IModelRetriever
{
    bool CanHandle(AiProvider provider);

    Task<IReadOnlyList<AiModel>> GetModelsAsync(AiProvider provider,
        CancellationToken cancellationToken = default);
}