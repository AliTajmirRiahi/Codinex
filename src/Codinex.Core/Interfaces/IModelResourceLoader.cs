using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IModelResourceLoader
    {
        Task<List<AiModel>> LoadAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default);
    }
}