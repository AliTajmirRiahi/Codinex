using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// Shows a proposed workspace change set to the user for review before it is applied.
/// </summary>
public interface IWorkspacePreviewService
{
    /// <summary>
    /// Computes the diff for every change in the set, shows the Code Changes review
    /// tool window, and returns the id the caller must pass to
    /// <see cref="IWorkspaceApprovalService.WaitForApprovalAsync"/>.
    /// </summary>
    Task<Guid> ShowAsync(
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken);
}
