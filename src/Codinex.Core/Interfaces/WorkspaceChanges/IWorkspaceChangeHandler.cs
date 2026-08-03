using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;

namespace Codify.Core.Interfaces.WorkspaceChanges;

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