using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Validation.Rules;

public sealed class ConflictValidationRule : IWorkspaceChangeValidationRule
{
    public Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default)
    {
        if (workspaceChangeSet == null)
            throw new ArgumentNullException(nameof(workspaceChangeSet));

        var result = WorkspaceValidationResult.Successful();

        var changeIds = new HashSet<Guid>();

        var createdFiles = new HashSet<string>(StringComparer.Ordinal);
        var deletedFiles = new HashSet<string>(StringComparer.Ordinal);

        var createdDirectories = new HashSet<string>(StringComparer.Ordinal);
        var deletedDirectories = new HashSet<string>(StringComparer.Ordinal);

        var editedFiles = new HashSet<string>(StringComparer.Ordinal);

        var renamedFiles = new HashSet<string>(StringComparer.Ordinal);
        var movedFiles = new HashSet<string>(StringComparer.Ordinal);

        var renamedDirectories = new HashSet<string>(StringComparer.Ordinal);
        var movedDirectories = new HashSet<string>(StringComparer.Ordinal);

        foreach (var change in workspaceChangeSet.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!changeIds.Add(change.Id))
            {
                result.AddError(CreateError(
                    change.Id,
                    "Duplicate workspace change id detected."));

                return Task.FromResult(result);
            }

            switch (change)
            {
                case CreateFileChange createFile:
                    if (!createdFiles.Add(createFile.FilePath))
                    {
                        result.AddError(CreateError(
                            createFile.Id,
                            $"File '{createFile.FilePath}' is created more than once."));

                        return Task.FromResult(result);
                    }
                    break;

                case DeleteFileChange deleteFile:
                    if (!deletedFiles.Add(deleteFile.FilePath))
                    {
                        result.AddError(CreateError(
                            deleteFile.Id,
                            $"File '{deleteFile.FilePath}' is deleted more than once."));

                        return Task.FromResult(result);
                    }
                    break;

                case CreateDirectoryChange createDirectory:
                    if (!createdDirectories.Add(createDirectory.DirectoryPath))
                    {
                        result.AddError(CreateError(
                            createDirectory.Id,
                            $"Directory '{createDirectory.DirectoryPath}' is created more than once."));

                        return Task.FromResult(result);
                    }
                    break;

                case DeleteDirectoryChange deleteDirectory:
                    if (!deletedDirectories.Add(deleteDirectory.DirectoryPath))
                    {
                        result.AddError(CreateError(
                            deleteDirectory.Id,
                            $"Directory '{deleteDirectory.DirectoryPath}' is deleted more than once."));

                        return Task.FromResult(result);
                    }
                    break;

                case EditFileChange editFile:
                    if (!editedFiles.Add(editFile.FilePath))
                    {
                        result.AddError(CreateError(
                            editFile.Id,
                            $"File '{editFile.FilePath}' is edited more than once."));

                        return Task.FromResult(result);
                    }
                    break;

                case RenameFileChange renameFile:
                    if (!renamedFiles.Add(renameFile.NewFileName))
                    {
                        result.AddError(CreateError(
                            renameFile.Id,
                            $"File '{renameFile.NewFileName}' is the target of multiple rename operations."));

                        return Task.FromResult(result);
                    }
                    break;

                case MoveFileChange moveFile:
                    if (!movedFiles.Add(moveFile.DestinationPath))
                    {
                        result.AddError(CreateError(
                            moveFile.Id,
                            $"File '{moveFile.DestinationPath}' is the destination of multiple move operations."));

                        return Task.FromResult(result);
                    }
                    break;

                case RenameDirectoryChange renameDirectory:
                    if (!renamedDirectories.Add(renameDirectory.NewPath))
                    {
                        result.AddError(CreateError(
                            renameDirectory.Id,
                            $"Directory '{renameDirectory.NewPath}' is the target of multiple rename operations."));

                        return Task.FromResult(result);
                    }
                    break;

                case MoveDirectoryChange moveDirectory:
                    if (!movedDirectories.Add(moveDirectory.DestinationPath))
                    {
                        result.AddError(CreateError(
                            moveDirectory.Id,
                            $"Directory '{moveDirectory.DestinationPath}' is the destination of multiple move operations."));

                        return Task.FromResult(result);
                    }
                    break;
            }
        }


        ValidateCircularFileRenames(workspaceChangeSet, result);
        if (!result.Success)
            return Task.FromResult(result);

        ValidateCircularFileMoves(workspaceChangeSet, result);
        if (!result.Success)
            return Task.FromResult(result);

        ValidateCircularDirectoryRenames(workspaceChangeSet, result);
        if (!result.Success)
            return Task.FromResult(result);

        ValidateCircularDirectoryMoves(workspaceChangeSet, result);
        return Task.FromResult(result);
    }
    private static void ValidateCircularFileRenames(
        WorkspaceChangeSet workspaceChangeSet,
        WorkspaceValidationResult result)
    {
        var map = new Dictionary<string, (Guid Id, string Destination)>(StringComparer.Ordinal);

        foreach (var change in workspaceChangeSet.Changes.OfType<RenameFileChange>())
            map[change.FilePath] = (change.Id, change.NewFileName);

        ValidateCircularMap(map, result);
    }

    private static void ValidateCircularFileMoves(
        WorkspaceChangeSet workspaceChangeSet,
        WorkspaceValidationResult result)
    {
        var map = new Dictionary<string, (Guid Id, string Destination)>(StringComparer.Ordinal);

        foreach (var change in workspaceChangeSet.Changes.OfType<MoveFileChange>())
            map[change.SourcePath] = (change.Id, change.DestinationPath);

        ValidateCircularMap(map, result);
    }

    private static void ValidateCircularDirectoryRenames(
        WorkspaceChangeSet workspaceChangeSet,
        WorkspaceValidationResult result)
    {
        var map = new Dictionary<string, (Guid Id, string Destination)>(StringComparer.Ordinal);

        foreach (var change in workspaceChangeSet.Changes.OfType<RenameDirectoryChange>())
            map[change.OldPath] = (change.Id, change.NewPath);

        ValidateCircularMap(map, result);
    }

    private static void ValidateCircularDirectoryMoves(
        WorkspaceChangeSet workspaceChangeSet,
        WorkspaceValidationResult result)
    {
        var map = new Dictionary<string, (Guid Id, string Destination)>(StringComparer.Ordinal);

        foreach (var change in workspaceChangeSet.Changes.OfType<MoveDirectoryChange>())
            map[change.SourcePath] = (change.Id, change.DestinationPath);

        ValidateCircularMap(map, result);
    }

    private static void ValidateCircularMap(
        Dictionary<string, (Guid Id, string Destination)> map,
        WorkspaceValidationResult result)
    {
        foreach (var start in map.Keys)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);

            var current = start;

            while (map.TryGetValue(current, out var node))
            {
                if (!visited.Add(current))
                {
                    result.AddError(CreateError(
                        node.Id,
                        "Circular workspace change detected."));

                    return;
                }

                current = node.Destination;
            }
        }
    }
    private static WorkspaceValidationError CreateError(
        Guid changeId,
        string message)
    {
        return new WorkspaceValidationError(changeId, WorkspaceChangeErrorCode.InvalidChange,
            WorkspaceValidationCategory.AiRecoverable, message);
    }
}