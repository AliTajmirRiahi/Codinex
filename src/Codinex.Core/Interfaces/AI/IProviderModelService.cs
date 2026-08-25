using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.AI;

public interface IProviderModelService
{
    Task<List<AiModel>> GetModelsAsync(AiProvider provider,
        CancellationToken cancellationToken = default);

    Task<List<AiModel>> GetModelsFromServerAsync(AiProvider provider,
        CancellationToken cancellationToken = default);
}