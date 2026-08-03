using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces;

public interface IProviderModelService
{
    Task<List<AiModel>> GetModelsAsync(AiProvider provider,
        CancellationToken cancellationToken = default);
}