using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Provides Git context for the current workspace.
    /// </summary>
    public interface IGitContextProvider
    {
        Task<GitContext> GetContextAsync(
            CancellationToken cancellationToken);
    }
}