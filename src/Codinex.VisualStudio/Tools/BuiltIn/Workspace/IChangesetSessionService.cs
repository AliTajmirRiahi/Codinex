using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// Owns the lifecycle of the current solution's changeset review: showing it,
/// persisting it to disk so it survives closing the tool window or Visual
/// Studio, blocking chat while it's pending, and applying the user's eventual
/// decision — whether that decision comes from the same live AI tool call or,
/// after a restart, from a review with no AI conversation still attached to it.
/// </summary>
public interface IChangesetSessionService
{
    /// <summary>True while a changeset review has not yet been decided for the current solution.</summary>
    bool HasPending { get; }

    /// <summary>
    /// Shows and persists a new review, blocks chat, waits for the decision (or for it to be marked
    /// undecided via <see cref="MarkUndecidedIfPending"/>), applies the approved subset, and returns
    /// the outcome for the caller (normally <see cref="ChangeSetCreatorTool"/>) to report.
    /// </summary>
    Task<ChangesetOutcome> RunReviewAsync(
        WorkspaceChangeSet changeSet,
        ChangeValidationResult resolutionResult,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called when a decision arrives from the review UI. If a live <see cref="RunReviewAsync"/> call
    /// is awaiting this id, resolves it and lets that call finalize. Otherwise (the review was restored
    /// after a restart, so nothing is awaiting it) finalizes it directly here.
    /// </summary>
    Task SubmitDecisionAsync(Guid changesetId, ChangesetDecision decision);

    /// <summary>
    /// Runs once at startup: if a review was left pending for the current solution, re-shows it and
    /// blocks chat again, without waiting on anything (the decision arrives later via
    /// <see cref="SubmitDecisionAsync"/>).
    /// </summary>
    Task TryRestorePendingReviewAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called when the Code Changes tool window or Visual Studio itself closes while a review is
    /// pending. Resolves any live wait with "undecided" so the awaiting tool call ends cleanly, without
    /// touching persistence or the pending/blocked state — the review is simply picked up again later.
    /// </summary>
    void MarkUndecidedIfPending();

    /// <summary>
    /// Re-shows the currently pending review (if any) — e.g. the user closed the tool window without
    /// deciding, leaving chat blocked with no visible way back to it. Safe to call even if nothing is
    /// pending (no-op) or if the window is already open (just re-focuses/re-syncs it).
    /// </summary>
    Task ReopenPendingReviewAsync(CancellationToken cancellationToken);
}
