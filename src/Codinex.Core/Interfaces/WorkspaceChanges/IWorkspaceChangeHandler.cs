using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Handles a specific type of workspace change.
/// </summary>
public interface IWorkspaceChangeHandler<in TChange>
    where TChange : WorkspaceChange
{
    Task<WorkspaceChangeResult> HandleAsync(
        TChange change,
        CancellationToken cancellationToken);
}