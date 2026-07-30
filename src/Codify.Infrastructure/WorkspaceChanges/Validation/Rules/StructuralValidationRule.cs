using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Validation.Rules;

public sealed class StructuralValidationRule : IWorkspaceChangeValidationRule
{
    public Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default)
    {
        if (workspaceChangeSet == null)
            throw new ArgumentNullException(nameof(workspaceChangeSet));

        var result = WorkspaceValidationResult.Successful();

        if (workspaceChangeSet.Changes.Count == 0)
        {
            result.AddError(CreateError(
                Guid.Empty,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "WorkspaceChangeSet must contain at least one change."));

            return Task.FromResult(result);
        }

        foreach (var change in workspaceChangeSet.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (change == null)
            {
                result.AddError(CreateError(
                    Guid.Empty,
                    WorkspaceChangeErrorCode.InvalidChange,
                    WorkspaceValidationCategory.AiRecoverable,
                    "WorkspaceChange cannot be null."));

                return Task.FromResult(result);
            }

            if (change.Id == Guid.Empty)
            {
                result.AddError(CreateError(
                    Guid.Empty,
                    WorkspaceChangeErrorCode.InvalidChange,
                    WorkspaceValidationCategory.AiRecoverable,
                    "WorkspaceChange.Id cannot be empty."));

                return Task.FromResult(result);
            }

            switch (change)
            {
                case EditFileChange editFileChange:
                    ValidateEditFileChange(editFileChange, result);
                    break;

                case CreateFileChange createFileChange:
                    ValidateCreateFileChange(createFileChange, result);
                    break;

                case DeleteFileChange deleteFileChange:
                    ValidateDeleteFileChange(deleteFileChange, result);
                    break;

                case RenameFileChange renameFileChange:
                    ValidateRenameFileChange(renameFileChange, result);
                    break;

                case MoveFileChange moveFileChange:
                    ValidateMoveFileChange(moveFileChange, result);
                    break;

                case CreateDirectoryChange createDirectoryChange:
                    ValidateCreateDirectoryChange(createDirectoryChange, result);
                    break;

                case DeleteDirectoryChange deleteDirectoryChange:
                    ValidateDeleteDirectoryChange(deleteDirectoryChange, result);
                    break;

                case RenameDirectoryChange renameDirectoryChange:
                    ValidateRenameDirectoryChange(renameDirectoryChange, result);
                    break;

                case MoveDirectoryChange moveDirectoryChange:
                    ValidateMoveDirectoryChange(moveDirectoryChange, result);
                    break;

                default:
                    result.AddError(CreateError(
                        change.Id,
                        WorkspaceChangeErrorCode.InvalidChange,
                        WorkspaceValidationCategory.AiRecoverable,
                        $"Unsupported workspace change type '{change.GetType().Name}'."));
                    break;
            }

            if (!result.Success)
                return Task.FromResult(result);
        }

        return Task.FromResult(result);
    }

    private static void ValidateEditFileChange(
        EditFileChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "FilePath is required."));
            return;
        }

        if (change.TextChanges.Count == 0)
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "EditFileChange must contain at least one text change."));
        }
    }

    private static void ValidateCreateFileChange(
        CreateFileChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "FilePath is required."));
        }
    }

    private static void ValidateDeleteFileChange(
        DeleteFileChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "FilePath is required."));
        }
    }

    private static void ValidateRenameFileChange(
        RenameFileChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "FilePath is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(change.NewFileName))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "NewName is required."));
        }
    }

    private static void ValidateMoveFileChange(
        MoveFileChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.SourcePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "SourcePath is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(change.DestinationPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "DestinationPath is required."));
        }
    }

    private static void ValidateCreateDirectoryChange(
        CreateDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.DirectoryPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "DirectoryPath is required."));
        }
    }

    private static void ValidateDeleteDirectoryChange(
        DeleteDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.DirectoryPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "DirectoryPath is required."));
        }
    }

    private static void ValidateRenameDirectoryChange(
        RenameDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.NewPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "NewPath is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(change.OldPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "OldPath is required."));
        }
    }

    private static void ValidateMoveDirectoryChange(
        MoveDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(change.SourcePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "DirectoryPath is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(change.DestinationPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.InvalidChange,
                WorkspaceValidationCategory.AiRecoverable,
                "DestinationPath is required."));
        }
    }

    private static WorkspaceValidationError CreateError(
        Guid changeId,
        WorkspaceChangeErrorCode code,
        WorkspaceValidationCategory category,
        string message)
    {
        return new WorkspaceValidationError(changeId, code,
            category, message);
    }
}