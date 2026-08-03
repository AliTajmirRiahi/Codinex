using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    public interface IBuildContextProvider
    {
        Task<BuildContext> GetContextAsync(
            CancellationToken cancellationToken);
    }
}