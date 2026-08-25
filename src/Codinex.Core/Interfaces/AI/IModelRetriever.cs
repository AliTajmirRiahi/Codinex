using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.AI;

public interface IModelRetriever
{
    bool CanHandle(AiProvider provider);

    Task<IReadOnlyList<AiModel>> GetModelsAsync(AiProvider provider,
        CancellationToken cancellationToken = default);
}