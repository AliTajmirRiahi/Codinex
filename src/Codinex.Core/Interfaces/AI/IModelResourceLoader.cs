using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.AI
{
    public interface IModelResourceLoader
    {
        Task<List<AiModel>> LoadAsync(
            AiProvider provider,
            CancellationToken cancellationToken = default);
    }
}