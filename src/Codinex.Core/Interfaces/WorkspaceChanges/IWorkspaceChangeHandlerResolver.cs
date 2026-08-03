using Codify.Core.Models.WorkspaceChanges;

namespace Codify.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Resolves the appropriate handler for a workspace change.
/// </summary>
public interface IWorkspaceChangeHandlerResolver
{
    IWorkspaceChangeHandler<TChange> Resolve<TChange>()
        where TChange : WorkspaceChange;
}