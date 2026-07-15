using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces;

public interface IProviderModelService
{
    Task<List<AiModel>> GetModelsAsync(AiProvider provider,
        CancellationToken cancellationToken = default);
}