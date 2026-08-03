using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Provides project information for the current solution.
    /// </summary>
    public interface IProjectContextProvider
    {
        Task<ProjectContext> GetContextAsync(
            CancellationToken cancellationToken);
    }
}