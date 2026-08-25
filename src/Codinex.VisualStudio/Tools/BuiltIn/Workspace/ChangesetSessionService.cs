using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Interfaces.Helper;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Services;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class ChangesetSessionService(
    IWorkspacePreviewService previewService,
    IWorkspaceApprovalService approvalService,
    IWorkspaceChangeApplier applier,
    IEditFileChangeResolutionContext resolutionContext,
    IStorageService storage,
    IWorkspaceContext workspaceContext,
    IUiThreadDispatcher uiThreadDispatcher,
    IWebViewClient webViewClient,
    IUserNotificationService userNotificationService)
    : IChangesetSessionService
{
    private Guid? _pendingId;
    private WorkspaceChangeSet _pendingChangeSet;
    private ChangeValidationResult _pendingResolution;

    public bool HasPending { get; private set; }

    public async Task<ChangesetOutcome> RunReviewAsync(
        WorkspaceChangeSet changeSet,
        ChangeValidationResult resolutionResult,
        CancellationToken cancellationToken)
    {
        var id = await previewService.ShowAsync(changeSet, resolutionResult, cancellationToken);

        await PersistAsync(new PendingChangesetRecord
        {
            Id = id,
            Summary = $"{changeSet.Changes.Count} change(s)",
            Changes = changeSet,
            ResolvedChanges = resolutionResult.Changes
        });

        _pendingId = id;
        _pendingChangeSet = changeSet;
        _pendingResolution = resolutionResult;
        HasPending = true;

        await NotifyChatBlockedAsync();

        var decision = await approvalService.WaitForApprovalAsync(id, cancellationToken);

        if (decision.IsUndecided)
        {
            // Still pending — leave persistence, HasPending, and the chat block exactly as they are.
            return ChangesetOutcome.Undecided();
        }

        return await FinalizeAsync(id, changeSet, resolutionResult, decision);
    }

    public async Task<ChangesetOutcome> ApplyDirectAsync(
        WorkspaceChangeSet changeSet,
        ChangeValidationResult resolutionResult)
    {
        if (changeSet == null)
            throw new ArgumentNullException(nameof(changeSet));

        if (resolutionResult == null)
            throw new ArgumentNullException(nameof(resolutionResult));

        var result = await ApplyApprovedChangesAsync(changeSet.Changes.ToList(), resolutionResult);

        return result.Success
            ? ChangesetOutcome.Applied(result.ChangeSuccess)
            : ChangesetOutcome.Failed(result.Error.Message, result.Error);
    }

    public async Task SubmitDecisionAsync(Guid changesetId, ChangesetDecision decision)
    {
        if (approvalService.TryResolveIfWaiting(changesetId, decision))
        {
            // A live RunReviewAsync call is awaiting this id — its own continuation will finalize.
            return;
        }

        // No live waiter: this decision is for a review restored after a Visual Studio restart.
        if (_pendingChangeSet == null || _pendingId != changesetId)
            return;

        var outcome = await FinalizeAsync(changesetId, _pendingChangeSet, _pendingResolution, decision);

        NotifyResumeOutcome(outcome);
    }

    public async Task TryRestorePendingReviewAsync(CancellationToken cancellationToken)
    {
        var record = await LoadPersistedAsync();

        if (record == null)
            return;

        var resolutionResult = ChangeValidationResult.Successful(record.ResolvedChanges);

        var id = await previewService.ShowAsync(record.Changes, resolutionResult, cancellationToken, record.Id);

        _pendingId = id;
        _pendingChangeSet = record.Changes;
        _pendingResolution = resolutionResult;
        HasPending = true;

        await NotifyChatBlockedAsync();
    }

    public void MarkUndecidedIfPending()
    {
        if (_pendingId == null)
            return;

        approvalService.TryResolveIfWaiting(_pendingId.Value, ChangesetDecision.Undecided());
    }

    public async Task ReopenPendingReviewAsync(CancellationToken cancellationToken)
    {
        if (_pendingChangeSet == null || _pendingId == null)
            return;

        // Re-shows the window and re-posts the diff (recomputed against current disk content) using
        // the same id — this doesn't touch the approval wait, so it's safe whether or not one exists.
        await previewService.ShowAsync(_pendingChangeSet, _pendingResolution, cancellationToken, _pendingId);
    }

    private async Task<ChangesetOutcome> FinalizeAsync(
        Guid id,
        WorkspaceChangeSet changeSet,
        ChangeValidationResult resolutionResult,
        ChangesetDecision decision)
    {
        var approvedChanges = changeSet.Changes
            .Where(change => decision.FileDecisions.TryGetValue(WorkspaceChangePathResolver.GetPath(change), out var approved) && approved)
            .ToList();

        var rejectedChanges = changeSet.Changes
            .Except(approvedChanges)
            .ToList();

        ChangesetOutcome outcome;

        if (approvedChanges.Count == 0)
        {
            outcome = ChangesetOutcome.Rejected(
                "The user rejected all proposed changes in the review window." +
                (string.IsNullOrWhiteSpace(decision.Reason) ? string.Empty : $" Rejection reason: {decision.Reason}") +
                " No files were created, edited, deleted, renamed, or moved.");
        }
        else
        {
            var result = await ApplyApprovedChangesAsync(approvedChanges, resolutionResult);

            if (!result.Success)
            {
                outcome = ChangesetOutcome.Failed(result.Error.Message, result.Error);
            }
            else
            {
                if (rejectedChanges.Count > 0)
                {
                    var rejectedPaths = rejectedChanges
                        .Select(WorkspaceChangePathResolver.GetPath)
                        .Distinct()
                        .ToList();

                    result.ChangeSuccess.Message =
                        $"{result.ChangeSuccess.Message} The user rejected these and they were NOT applied: {string.Join(", ", rejectedPaths)}." +
                        (string.IsNullOrWhiteSpace(decision.Reason) ? string.Empty : $" Rejection reason: {decision.Reason}");
                }

                outcome = ChangesetOutcome.Applied(result.ChangeSuccess);
            }
        }

        await ClearPendingAsync();

        return outcome;
    }

    /// <summary>
    /// Applies the approved subset of a changeset through the generic
    /// <see cref="IWorkspaceChangeApplier"/>, unchanged for every change kind. For
    /// EditFileChange specifically, <c>EditFileChangeHandler</c> still matches on Search
    /// first, against the freshly-read file; <paramref name="resolutionResult"/> is made
    /// available to it (via <see cref="IEditFileChangeResolutionContext"/>) purely as a
    /// fallback for when that no longer finds a unique match.
    /// </summary>
    private async Task<WorkspaceChangeResult> ApplyApprovedChangesAsync(
        List<WorkspaceChange> approvedChanges,
        ChangeValidationResult resolutionResult)
    {
        resolutionContext.Current = resolutionResult;

        try
        {
            return await applier.ApplyAsync(new WorkspaceChangeSet { Changes = approvedChanges });
        }
        finally
        {
            resolutionContext.Current = null;
        }
    }

    private async Task ClearPendingAsync()
    {
        _pendingId = null;
        _pendingChangeSet = null;
        HasPending = false;

        await DeletePersistedAsync();
        await NotifyChatUnblockedAsync();
    }

    private void NotifyResumeOutcome(ChangesetOutcome outcome)
    {
        // No AI conversation is waiting on a restored review's outcome — tell the user directly.
        var message = outcome.Kind switch
        {
            ChangesetOutcomeKind.Applied => $"Codinex AI: {outcome.Message}",
            ChangesetOutcomeKind.Rejected => $"Codinex AI: {outcome.Message}",
            ChangesetOutcomeKind.Error => $"Codinex AI: Failed to apply the reviewed changes. {outcome.Message}",
            _ => null
        };

        if (message == null)
            return;

        if (outcome.Kind == ChangesetOutcomeKind.Error)
        {
            userNotificationService.ShowError(message);
        }
        else
        {
            userNotificationService.ShowInfo(message);
        }
    }

    private Task NotifyChatBlockedAsync() =>
        SafePostAsync(new { type = WebViewMessageType.ChatBlocked, payload = new { } });

    private Task NotifyChatUnblockedAsync() =>
        SafePostAsync(new { type = WebViewMessageType.ChatUnblocked, payload = new { } });

    private async Task SafePostAsync(object message)
    {
        try
        {
            await webViewClient.PostMessageAsync(message);
        }
        catch
        {
            // The chat window may not be open yet; it picks up the current blocked
            // state from its own INIT_DATA payload when it does open.
        }
    }

    private async Task<string> GetPendingChangesetPathAsync()
    {
        await uiThreadDispatcher.SwitchToMainThreadAsync();

        var workspaceName = workspaceContext.IsSolutionOpen && !string.IsNullOrWhiteSpace(workspaceContext.SolutionName)
            ? workspaceContext.SolutionName
            : "DefaultSolution";

        return StoragePaths.GetPendingChangesetPath(workspaceName);
    }

    private async Task PersistAsync(PendingChangesetRecord record)
    {
        var path = await GetPendingChangesetPathAsync();

        await storage.SaveAsync(path, record);
    }

    private async Task<PendingChangesetRecord> LoadPersistedAsync()
    {
        var path = await GetPendingChangesetPathAsync();

        if (!await storage.ExistsAsync(path))
            return null;

        try
        {
            return await storage.LoadAsync<PendingChangesetRecord>(path);
        }
        catch (Exception)
        {
            // Corrupt or legacy (pre-$type) file — it can never load, so drop it
            // rather than fail every startup from now on.
            await storage.DeleteAsync(path);
            return null;
        }
    }

    private async Task DeletePersistedAsync()
    {
        var path = await GetPendingChangesetPathAsync();

        await storage.DeleteAsync(path);
    }
}
