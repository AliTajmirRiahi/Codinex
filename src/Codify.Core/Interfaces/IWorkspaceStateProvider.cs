using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Workspace;

namespace Codify.Core.Interfaces
{
    public interface IWorkspaceStateProvider
    {
        Task<WorkspaceState> GetAsync(
            CancellationToken cancellationToken);
    }
}
