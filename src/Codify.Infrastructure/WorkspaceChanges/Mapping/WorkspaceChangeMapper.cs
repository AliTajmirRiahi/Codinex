using System;
using System.Linq;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;

namespace Codify.Infrastructure.WorkspaceChanges.Mapping;

/// <summary>
/// Maps workspace change DTOs to domain models.
/// </summary>
[AutoDiRegister(Modules.JSON, RegistrationOrder.Foundation)]
internal sealed class WorkspaceChangeMapper : IWorkspaceChangeMapper
{
    public WorkspaceChangeSet Map(WorkspaceChangeSetDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new WorkspaceChangeSet
        {
            Changes = dto.Changes
                .Select(Map)
                .ToList()
        };
    }

    private static WorkspaceChange Map(WorkspaceChangeDto dto)
    {
        return dto switch
        {
            CreateFileChangeDto change => Map(change),
            EditFileChangeDto change => Map(change),
            DeleteFileChangeDto change => Map(change),
            RenameFileChangeDto change => Map(change),
            MoveFileChangeDto change => Map(change),

            CreateDirectoryChangeDto change => Map(change),
            DeleteDirectoryChangeDto change => Map(change),
            RenameDirectoryChangeDto change => Map(change),
            MoveDirectoryChangeDto change => Map(change),

            _ => throw new NotSupportedException(
                $"Unsupported workspace change DTO '{dto.GetType().Name}'.")
        };
    }

    private static CreateFileChange Map(CreateFileChangeDto dto)
    {
        return new CreateFileChange
        {
            FilePath = dto.Path,
            Content = dto.Content
        };
    }

    private static EditFileChange Map(EditFileChangeDto dto)
    {
        return new EditFileChange
        {
            FilePath = dto.Path,
            TextChanges = dto.Changes
                .Select(Map)
                .ToList()
        };
    }

    private static DeleteFileChange Map(DeleteFileChangeDto dto)
    {
        return new DeleteFileChange
        {
            FilePath = dto.Path
        };
    }

    private static RenameFileChange Map(RenameFileChangeDto dto)
    {
        return new RenameFileChange
        {
            FilePath = dto.Path,
            NewFileName = dto.NewName
        };
    }

    private static MoveFileChange Map(MoveFileChangeDto dto)
    {
        return new MoveFileChange
        {
            SourcePath = dto.Source,
            DestinationPath = dto.Destination
        };
    }

    private static CreateDirectoryChange Map(CreateDirectoryChangeDto dto)
    {
        return new CreateDirectoryChange
        {
            DirectoryPath = dto.Path
        };
    }

    private static DeleteDirectoryChange Map(DeleteDirectoryChangeDto dto)
    {
        return new DeleteDirectoryChange
        {
            DirectoryPath = dto.Path
        };
    }

    private static RenameDirectoryChange Map(RenameDirectoryChangeDto dto)
    {
        return new RenameDirectoryChange
        {
            OldPath = dto.Path,
            NewPath = dto.NewName
        };
    }

    private static MoveDirectoryChange Map(MoveDirectoryChangeDto dto)
    {
        return new MoveDirectoryChange
        {
            SourcePath = dto.Source,
            DestinationPath = dto.Destination
        };
    }

    private static TextFileChange Map(TextFileChangeDto dto)
    {
        return new TextFileChange
        {
            Id = dto.Id,
            Order = dto.Order,
            Before = dto.Before,
            Search = dto.Search,
            Replace = dto.Replace,
            After = dto.After
        };
    }
}