using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.Helper;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// Computes diffs for a proposed workspace change set and shows them in the
/// Code Changes review tool window.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class WorkspacePreviewService(
    IWorkspaceFileService workspaceFileService,
    ITextChangeMatcher textChangeMatcher,
    ITextChangeApplier textChangeApplier,
    IStringHelper stringHelper,
    IChangeReviewWebViewClient changeReviewWebViewClient,
    IVisualStudioServices visualStudioServices)
    : IWorkspacePreviewService
{
    public async Task<Guid> ShowAsync(
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken,
        Guid? existingId = null)
    {
        if (changeSet == null)
            throw new ArgumentNullException(nameof(changeSet));

        var id = existingId ?? Guid.NewGuid();

        var files = new List<ChangesetFileDiff>();

        foreach (var change in changeSet.Changes)
        {
            files.Add(await BuildDiffAsync(change, cancellationToken));
        }

        var model = new WorkspacePreviewModel
        {
            Id = id,
            Summary = BuildSummary(files),
            Files = files
        };

        await visualStudioServices.ShowToolWindowAsync(new Guid(ToolWindowGuids.CodeChangesToolWindow));

        await changeReviewWebViewClient.PostMessageAsync(new
        {
            type = WebViewMessageType.ChangesetShow,
            payload = model
        });

        return id;
    }

    private async Task<ChangesetFileDiff> BuildDiffAsync(
        WorkspaceChange change,
        CancellationToken cancellationToken)
    {
        switch (change)
        {
            case EditFileChange edit:
                return await BuildEditFileDiffAsync(edit, cancellationToken);

            case CreateFileChange create:
                return new ChangesetFileDiff
                {
                    FilePath = create.FilePath,
                    Operation = "CreateFile",
                    OriginalText = string.Empty,
                    ModifiedText = create.Content ?? string.Empty
                };

            case DeleteFileChange delete:
                return new ChangesetFileDiff
                {
                    FilePath = delete.FilePath,
                    Operation = "DeleteFile",
                    OriginalText = await ReadSafeAsync(delete.FilePath, cancellationToken),
                    ModifiedText = string.Empty
                };

            case RenameFileChange rename:
                return new ChangesetFileDiff
                {
                    FilePath = rename.FilePath,
                    Operation = "RenameFile",
                    OriginalText = rename.FilePath,
                    ModifiedText = rename.NewFileName
                };

            case MoveFileChange move:
                return new ChangesetFileDiff
                {
                    FilePath = move.SourcePath,
                    Operation = "MoveFile",
                    OriginalText = move.SourcePath,
                    ModifiedText = move.DestinationPath
                };

            case CreateDirectoryChange createDirectory:
                return new ChangesetFileDiff
                {
                    FilePath = WorkspaceChangePathResolver.GetPath(createDirectory),
                    Operation = "CreateDirectory"
                };

            case DeleteDirectoryChange deleteDirectory:
                return new ChangesetFileDiff
                {
                    FilePath = WorkspaceChangePathResolver.GetPath(deleteDirectory),
                    Operation = "DeleteDirectory"
                };

            case RenameDirectoryChange renameDirectory:
                return new ChangesetFileDiff
                {
                    FilePath = renameDirectory.OldPath,
                    Operation = "RenameDirectory",
                    OriginalText = renameDirectory.OldPath,
                    ModifiedText = renameDirectory.NewPath
                };

            case MoveDirectoryChange moveDirectory:
                return new ChangesetFileDiff
                {
                    FilePath = moveDirectory.SourcePath,
                    Operation = "MoveDirectory",
                    OriginalText = moveDirectory.SourcePath,
                    ModifiedText = moveDirectory.DestinationPath
                };

            default:
                return new ChangesetFileDiff
                {
                    FilePath = string.Empty,
                    Operation = change.Kind.ToString()
                };
        }
    }

    private async Task<ChangesetFileDiff> BuildEditFileDiffAsync(
        EditFileChange change,
        CancellationToken cancellationToken)
    {
        var originalContent = await ReadSafeAsync(change.FilePath, cancellationToken);

        var modifiedContent = originalContent;
        string previewWarning = null;

        foreach (var textChange in change.TextChanges.OrderBy(x => x.Order))
        {
            var match = textChangeMatcher.Match(modifiedContent, textChange);

            if (match.Status != TextChangeMatchStatus.Success)
            {
                // Stop applying further edits, but say so — leaving this silent would render as
                // "no changes" for a file that actually has pending edits, which is misleading.
                previewWarning =
                    $"Could not preview edit #{textChange.Order}: {match.Status} " +
                    $"({match.Error ?? "the expected text was not found"}). " +
                    "The diff below reflects only the edits applied before this point; " +
                    "applying may fail or produce different results than shown.";
                break;
            }

            modifiedContent = textChangeApplier.Apply(modifiedContent, match, textChange);
        }

        return new ChangesetFileDiff
        {
            FilePath = change.FilePath,
            Operation = "EditFile",
            OriginalText = originalContent,
            ModifiedText = modifiedContent,
            PreviewWarning = previewWarning
        };
    }

    private async Task<string> ReadSafeAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var content = await workspaceFileService.ReadAsync(filePath, cancellationToken);

            return stringHelper.Normalize(content);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BuildSummary(IReadOnlyCollection<ChangesetFileDiff> files)
    {
        return $"{files.Count} file{(files.Count == 1 ? string.Empty : "s")} changed";
    }
}
