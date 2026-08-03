using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
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