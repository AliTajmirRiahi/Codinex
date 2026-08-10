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
    /// Awaits the user's decision for the given change set id.
    /// </summary>
    Task<bool> WaitForApprovalAsync(
        Guid changesetId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records the user's decision, unblocking the matching <see cref="WaitForApprovalAsync"/> call.
    /// </summary>
    void SetDecision(Guid changesetId, bool approved);
}
