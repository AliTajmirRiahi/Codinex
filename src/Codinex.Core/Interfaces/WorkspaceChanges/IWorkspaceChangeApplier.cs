using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Applies workspace changes to the project.
/// </summary>
public interface IWorkspaceChangeApplier
{
    Task<WorkspaceChangeResult> ApplyAsync(
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken = default);
}