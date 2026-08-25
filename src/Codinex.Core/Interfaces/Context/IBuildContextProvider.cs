using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.Context;

namespace Codinex.Core.Interfaces.Context
{
    public interface IBuildContextProvider
    {
        Task<BuildContext> GetContextAsync(
            CancellationToken cancellationToken);
    }
}