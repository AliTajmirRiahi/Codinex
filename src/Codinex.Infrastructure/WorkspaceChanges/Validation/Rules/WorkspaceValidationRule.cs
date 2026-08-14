using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Infrastructure.WorkspaceChanges.Validation.Rules;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class WorkspaceStateValidationRule(
    IWorkspaceContext workspaceContext)
    : IWorkspaceChangeValidationRule
{

    private static IWorkspaceContext _workspaceContext = null;

    public Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default)
    {
        if (workspaceChangeSet == null)
            throw new ArgumentNullException(nameof(workspaceChangeSet));

        _workspaceContext = workspaceContext;

        var result = WorkspaceValidationResult.Successful();

        var workspaceRoot = Path.GetFullPath(workspaceContext.SolutionDirectory);

        if (!workspaceRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            workspaceRoot += Path.DirectorySeparatorChar;
        }

        foreach (var change in workspaceChangeSet.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (change)
            {
                case CreateFileChange createFileChange:
                    ValidatePath(
                        createFileChange.Id,
                        createFileChange.FilePath,
                        workspaceRoot,
                        result);
                    break;

                case EditFileChange editFileChange:
                    ValidatePath(
                        editFileChange.Id,
                        editFileChange.FilePath,
                        workspaceRoot,
                        result);
                    break;

                case DeleteFileChange deleteFileChange:
                    ValidatePath(
                        deleteFileChange.Id,
                        deleteFileChange.FilePath,
                        workspaceRoot,
                        result);
                    break;

                case RenameFileChange renameFileChange:
                    ValidatePath(
                        renameFileChange.Id,
                        renameFileChange.FilePath,
                        workspaceRoot,
                        result);

                    ValidatePath(
                        renameFileChange.Id,
                        renameFileChange.NewFileName,
                        workspaceRoot,
                        result);
                    break;

                case MoveFileChange moveFileChange:
                    ValidatePath(
                        moveFileChange.Id,
                        moveFileChange.SourcePath,
                        workspaceRoot,
                        result);

                    ValidatePath(
                        moveFileChange.Id,
                        moveFileChange.DestinationPath,
                        workspaceRoot,
                        result);
                    break;

                case CreateDirectoryChange createDirectoryChange:
                    ValidatePath(
                        createDirectoryChange.Id,
                        createDirectoryChange.DirectoryPath,
                        workspaceRoot,
                        result);
                    break;

                case DeleteDirectoryChange deleteDirectoryChange:
                    ValidatePath(
                        deleteDirectoryChange.Id,
                        deleteDirectoryChange.DirectoryPath,
                        workspaceRoot,
                        result);
                    break;
                case RenameDirectoryChange renameDirectoryChange:
                    ValidatePath(
                        renameDirectoryChange.Id,
                        renameDirectoryChange.OldPath,
                        workspaceRoot,
                        result);

                    ValidatePath(
                        renameDirectoryChange.Id,
                        renameDirectoryChange.NewPath,
                        workspaceRoot,
                        result);
                    break;

                case MoveDirectoryChange moveDirectoryChange:
                    ValidatePath(
                        moveDirectoryChange.Id,
                        moveDirectoryChange.SourcePath,
                        workspaceRoot,
                        result);

                    ValidatePath(
                        moveDirectoryChange.Id,
                        moveDirectoryChange.DestinationPath,
                        workspaceRoot,
                        result);
                    break;
            }

            if (!result.Success)
                return Task.FromResult(result);
        }

        return Task.FromResult(result);
    }

    private static void ValidatePath(
        Guid changeId,
        string path,
        string workspaceRoot,
        WorkspaceValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            result.AddError(CreateError(
                changeId,
                WorkspaceChangeErrorCode.InvalidPath,
                WorkspaceValidationCategory.AiRecoverable,
                $"'{path}' contains invalid path characters."));

            return;
        }

        if (Path.IsPathRooted(path) && !path.Contains(_workspaceContext.SolutionPath) && !path.Contains(_workspaceContext.SolutionDirectory))
        {
            result.AddError(CreateError(
                changeId,
                WorkspaceChangeErrorCode.AbsolutePathNotAllowed,
                WorkspaceValidationCategory.AiRecoverable,
                $"Absolute path '{path}' is not allowed."));

            return;
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(workspaceRoot, path));

        if (!fullPath.StartsWith(
                workspaceRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            result.AddError(CreateError(
                changeId,
                WorkspaceChangeErrorCode.PathOutsideWorkspace,
                WorkspaceValidationCategory.AiRecoverable,
                $"Path '{path}' resolves outside the workspace."));
        }
    }

    private static WorkspaceValidationError CreateError(
        Guid changeId,
        WorkspaceChangeErrorCode code,
        WorkspaceValidationCategory category,
        string message)
    {
        return new WorkspaceValidationError(
            changeId,
            code,
            category,
            message);
    }
}