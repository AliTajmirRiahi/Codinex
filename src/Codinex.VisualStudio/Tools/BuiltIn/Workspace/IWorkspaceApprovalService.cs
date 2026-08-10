using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// Waits for the user's Accept/Reject decision on a change set shown via
/// <see cref="IWorkspacePreviewService"/>.
/// </summary>
public interface IWorkspaceApprovalService
{
    /// <summary>
    /// Awaits the user's per-file decision for the given change set id.
    /// </summary>
    Task<ChangesetDecision> WaitForApprovalAsync(
        Guid changesetId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the matching <see cref="WaitForApprovalAsync"/> call for this changeset id, if one is
    /// currently awaiting it. Returns false (no-op) when nothing is waiting — e.g. the changeset was
    /// restored from disk after a Visual Studio restart, so no live call is blocked on it.
    /// </summary>
    bool TryResolveIfWaiting(Guid changesetId, ChangesetDecision decision);
}
