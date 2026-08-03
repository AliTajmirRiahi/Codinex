using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;

namespace Codify.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Applies workspace changes to the project.
/// </summary>
public interface IWorkspaceChangeApplier
{
    Task<WorkspaceChangeResult> ApplyAsync(
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken = default);
}