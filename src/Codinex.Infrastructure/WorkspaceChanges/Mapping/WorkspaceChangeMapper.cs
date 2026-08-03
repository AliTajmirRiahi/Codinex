using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces.Helper;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Parsing.Dtos;
using System;
using System.Linq;

namespace Codify.Infrastructure.WorkspaceChanges.Mapping;

/// <summary>
/// Maps workspace change DTOs to domain models.
/// </summary>
[AutoDiRegister(Modules.JSON, RegistrationOrder.Foundation)]
internal sealed class WorkspaceChangeMapper(IStringHelper stringHelper) : IWorkspaceChangeMapper
{
    private readonly IStringHelper _stringHelper = stringHelper;

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

    private WorkspaceChange Map(WorkspaceChangeDto dto)
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

    private CreateFileChange Map(CreateFileChangeDto dto)
    {
        return new CreateFileChange
        {
            FilePath = dto.FilePath,
            Content = dto.Content
        };
    }

    private EditFileChange Map(EditFileChangeDto dto)
    {
        return new EditFileChange
        {
            FilePath = dto.FilePath,
            TextChanges = dto.TextChanges
                .Select(Map)
                .ToList()
        };
    }

    private DeleteFileChange Map(DeleteFileChangeDto dto)
    {
        return new DeleteFileChange
        {
            FilePath = dto.FilePath
        };
    }

    private RenameFileChange Map(RenameFileChangeDto dto)
    {
        return new RenameFileChange
        {
            FilePath = dto.FilePath,
            NewFileName = dto.NewFileName
        };
    }

    private MoveFileChange Map(MoveFileChangeDto dto)
    {
        return new MoveFileChange
        {
            SourcePath = dto.SourcePath,
            DestinationPath = dto.DestinationPath
        };
    }

    private CreateDirectoryChange Map(CreateDirectoryChangeDto dto)
    {
        return new CreateDirectoryChange
        {
            DirectoryPath = dto.DirectoryPath
        };
    }

    private DeleteDirectoryChange Map(DeleteDirectoryChangeDto dto)
    {
        return new DeleteDirectoryChange
        {
            DirectoryPath = dto.DirectoryPath
        };
    }

    private RenameDirectoryChange Map(RenameDirectoryChangeDto dto)
    {
        return new RenameDirectoryChange
        {
            OldPath = dto.OldPath,
            NewPath = dto.NewPath
        };
    }

    private MoveDirectoryChange Map(MoveDirectoryChangeDto dto)
    {
        return new MoveDirectoryChange
        {
            SourcePath = dto.SourcePath,
            DestinationPath = dto.DestinationPath
        };
    }

    private TextFileChange Map(TextFileChangeDto dto)
    {
        return new TextFileChange
        {
            Id = dto.Id,
            Order = dto.Order,
            Before = _stringHelper.Normalize(dto.Before),
            Search = _stringHelper.Normalize(dto.Search),
            Replace = _stringHelper.Normalize(dto.Replace),
            After = _stringHelper.Normalize(dto.After)
        };
    }
}