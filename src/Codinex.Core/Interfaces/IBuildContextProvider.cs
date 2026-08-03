using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IBuildContextProvider
    {
        Task<BuildContext> GetContextAsync(
            CancellationToken cancellationToken);
    }
}