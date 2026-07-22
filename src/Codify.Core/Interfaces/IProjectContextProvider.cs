using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
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