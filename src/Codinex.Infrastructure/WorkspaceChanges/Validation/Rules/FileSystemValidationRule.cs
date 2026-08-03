using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Validation.Rules;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class FileSystemValidationRule(IWorkspaceFileService workspaceFileService)
    : IWorkspaceChangeValidationRule
{
    public Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default)
    {
        if (workspaceChangeSet == null)
            throw new ArgumentNullException(nameof(workspaceChangeSet));

        var result = WorkspaceValidationResult.Successful();

        foreach (var change in workspaceChangeSet.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (change)
            {
                case CreateFileChange createFile:
                    ValidateCreateFile(createFile, result);
                    break;

                case EditFileChange editFile:
                    ValidateEditFile(editFile, result);
                    break;

                case DeleteFileChange deleteFile:
                    ValidateDeleteFile(deleteFile, result);
                    break;

                case RenameFileChange renameFile:
                    ValidateRenameFile(renameFile, result);
                    break;

                case MoveFileChange moveFile:
                    ValidateMoveFile(moveFile, result);
                    break;

                case CreateDirectoryChange createDirectory:
                    ValidateCreateDirectory(createDirectory, result);
                    break;

                case DeleteDirectoryChange deleteDirectory:
                    ValidateDeleteDirectory(deleteDirectory, result);
                    break;

                case RenameDirectoryChange renameDirectory:
                    ValidateRenameDirectory(renameDirectory, result);
                    break;

                case MoveDirectoryChange moveDirectory:
                    ValidateMoveDirectory(moveDirectory, result);
                    break;
            }

            if (!result.Success)
                return Task.FromResult(result);
        }

        return Task.FromResult(result);
    }

    private void ValidateCreateFile(
        CreateFileChange change,
        WorkspaceValidationResult result)
    {
        if (workspaceFileService.FileExists(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.FileAlreadyExists,
                $"File '{change.FilePath}' already exists."));
        }
    }

    private void ValidateEditFile(
        EditFileChange change,
        WorkspaceValidationResult result)
    {
        if (!workspaceFileService.FileExists(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.FileNotFound,
                $"File '{change.FilePath}' was not found."));
        }
    }

    private void ValidateDeleteFile(
        DeleteFileChange change,
        WorkspaceValidationResult result)
    {
        if (!workspaceFileService.FileExists(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.FileNotFound,
                $"File '{change.FilePath}' was not found."));
        }
    }

    private void ValidateRenameFile(
        RenameFileChange change,
        WorkspaceValidationResult result)
    {
        if (!workspaceFileService.FileExists(change.FilePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.FileNotFound,
                $"File '{change.FilePath}' was not found."));
        }
    }

    private void ValidateMoveFile(
        MoveFileChange change,
        WorkspaceValidationResult result)
    {
        if (!workspaceFileService.FileExists(change.SourcePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.FileNotFound,
                $"File '{change.SourcePath}' was not found."));
        }
    }

    private void ValidateCreateDirectory(
        CreateDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (workspaceFileService.DirectoryExists(change.DirectoryPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.DirectoryAlreadyExists,
                $"Directory '{change.DirectoryPath}' already exists."));
        }
    }

    private void ValidateDeleteDirectory(
        DeleteDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (!workspaceFileService.DirectoryExists(change.DirectoryPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.DirectoryNotFound,
                $"Directory '{change.DirectoryPath}' was not found."));
        }
    }

    private void ValidateRenameDirectory(
        RenameDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (!workspaceFileService.DirectoryExists(change.OldPath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.DirectoryNotFound,
                $"Directory '{change.OldPath}' was not found."));
        }
    }

    private void ValidateMoveDirectory(
        MoveDirectoryChange change,
        WorkspaceValidationResult result)
    {
        if (!workspaceFileService.DirectoryExists(change.SourcePath))
        {
            result.AddError(CreateError(
                change.Id,
                WorkspaceChangeErrorCode.DirectoryNotFound,
                $"Directory '{change.SourcePath}' was not found."));
        }
    }

    private static WorkspaceValidationError CreateError(
        Guid changeId,
        WorkspaceChangeErrorCode errorCode,
        string message)
    {
        return new WorkspaceValidationError(
            changeId,
            errorCode,
            WorkspaceValidationCategory.UserRecoverable,
            message);
    }
}