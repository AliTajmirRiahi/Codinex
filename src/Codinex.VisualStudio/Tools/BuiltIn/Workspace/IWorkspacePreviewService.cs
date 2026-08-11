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
    /// <param name="cancellationToken"></param>
    /// <param name="existingId">
    /// Pass the id of a changeset restored from a persisted <c>PendingChangesetRecord</c> so the
    /// re-shown review keeps the same id (e.g. for matching an incoming decision back to it).
    /// Leave null to generate a new id for a brand-new review.
    /// </param>
    /// <param name="changeSet"></param>
    /// <param name="resolutionResult"></param>
    Task<Guid> ShowAsync(
        WorkspaceChangeSet changeSet,
        ChangeValidationResult resolutionResult,
        CancellationToken cancellationToken,
        Guid? existingId = null);
}
