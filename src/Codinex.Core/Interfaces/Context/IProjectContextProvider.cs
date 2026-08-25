using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.Context;

namespace Codinex.Core.Interfaces.Context
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