using System;
using System.Collections.Generic;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;
using Newtonsoft.Json;

namespace Codify.Infrastructure.WorkspaceChanges.Parsing.Factories;

/// <summary>
/// Creates workspace change DTO instances based on the workspace change kind.
/// </summary>
internal static class WorkspaceChangeDtoFactory
{
    private static readonly IReadOnlyDictionary<WorkspaceChangeKind, Func<WorkspaceChangeDto>> Factories =
        new Dictionary<WorkspaceChangeKind, Func<WorkspaceChangeDto>>
        {
            { WorkspaceChangeKind.CreateFile, () => new CreateFileChangeDto() },
            { WorkspaceChangeKind.EditFile, () => new EditFileChangeDto() },
            { WorkspaceChangeKind.DeleteFile, () => new DeleteFileChangeDto() },
            { WorkspaceChangeKind.RenameFile, () => new RenameFileChangeDto() },
            { WorkspaceChangeKind.MoveFile, () => new MoveFileChangeDto() },
            { WorkspaceChangeKind.CreateDirectory, () => new CreateDirectoryChangeDto() },
            { WorkspaceChangeKind.DeleteDirectory, () => new DeleteDirectoryChangeDto() },
            { WorkspaceChangeKind.RenameDirectory, () => new RenameDirectoryChangeDto() },
            { WorkspaceChangeKind.MoveDirectory, () => new MoveDirectoryChangeDto() }
        };

    /// <summary>
    /// Creates a DTO for the specified workspace change kind.
    /// </summary>
    public static WorkspaceChangeDto Create(WorkspaceChangeKind kind)
    {
        if (!Factories.TryGetValue(kind, out var factory))
        {
            throw new JsonSerializationException(
                $"Unsupported workspace change kind '{kind}'.");
        }

        return factory();
    }
}