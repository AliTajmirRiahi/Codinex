using System;
using System.Collections.Generic;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace.Schemas;

internal static class WorkspaceToolSchemas
{
    
    private static ToolProperty KindSchema(WorkspaceChangeKind kind)
    {
        return new ToolProperty(
            ToolPropertyType.String,
            $"Must be '{kind}'.")
        {
            Enum = [kind.ToString()]
        };
    }

    public static readonly ToolProperty TextFileChangeProp =
        new(
            ToolPropertyType.Object,
            "Represents a single text modification.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(TextFileChange.Order)] = new(
                    ToolPropertyType.Integer,
                    "The execution order of this text change."),

                [nameof(TextFileChange.Before)] = new(
                    ToolPropertyType.String,
                    "Text expected immediately before the search match."),

                [nameof(TextFileChange.Search)] = new(
                    ToolPropertyType.String,
                    "The unique text to search for."),

                [nameof(TextFileChange.Replace)] = new(
                    ToolPropertyType.String,
                    "The replacement text."),

                [nameof(TextFileChange.After)] = new(
                    ToolPropertyType.String,
                    "Text expected immediately after the search match.")
            },

            Required =
            [
                nameof(TextFileChange.Order),
                nameof(TextFileChange.Search),
                nameof(TextFileChange.Replace),
                nameof(TextFileChange.Before),
                nameof(TextFileChange.After),
            ]
        };

    public static readonly ToolProperty CreateFileChangeProp =
        new(
            ToolPropertyType.Object,
            "Creates a new file.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(CreateFileChange.Kind)] = KindSchema(WorkspaceChangeKind.CreateFile),

                [nameof(CreateFileChange.FilePath)] = new(
                    ToolPropertyType.String,
                    "The path of the file to create."),

                [nameof(CreateFileChange.Content)] = new(
                    ToolPropertyType.String,
                    "The complete content of the new file.")
            },

            Required =
            [
                nameof(CreateFileChange.Kind),
                nameof(CreateFileChange.FilePath),
                nameof(CreateFileChange.Content)
            ]
        };

    public static readonly ToolProperty EditFileChangeProp =
        new(
            ToolPropertyType.Object,
            "Edits an existing file.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(EditFileChange.Kind)] = KindSchema(WorkspaceChangeKind.EditFile),

                [nameof(EditFileChange.FilePath)] = new(
                    ToolPropertyType.String,
                    "The path of the file to edit."),

                [nameof(EditFileChange.TextChanges)] = new(
                    ToolPropertyType.Array,
                    "Ordered list of text modifications.")
                {
                    Items = TextFileChangeProp
                }
            },

            Required =
            [
                nameof(EditFileChange.Kind),
                nameof(EditFileChange.FilePath),
                nameof(EditFileChange.TextChanges)
            ]
        };

    public static readonly ToolProperty DeleteFileChangeProp =
        new(
            ToolPropertyType.Object,
            "Deletes an existing file.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(DeleteFileChange.Kind)] = KindSchema(WorkspaceChangeKind.DeleteFile),

                [nameof(DeleteFileChange.FilePath)] = new(
                    ToolPropertyType.String,
                    "The path of the file to delete.")
            },

            Required =
            [
                nameof(DeleteFileChange.Kind),
                nameof(DeleteFileChange.FilePath)
            ]
        };

    public static readonly ToolProperty RenameFileChangeProp =
        new(
            ToolPropertyType.Object,
            "Renames an existing file.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(RenameFileChange.Kind)] = KindSchema(WorkspaceChangeKind.RenameFile),

                [nameof(RenameFileChange.FilePath)] = new(
                    ToolPropertyType.String,
                    "The path of the file to rename."),

                [nameof(RenameFileChange.NewFileName)] = new(
                    ToolPropertyType.String,
                    "The new file name without changing its directory.")
            },

            Required =
            [
                nameof(RenameFileChange.Kind),
                nameof(RenameFileChange.FilePath),
                nameof(RenameFileChange.NewFileName)
            ]
        };

    public static readonly ToolProperty MoveFileChangeProp =
        new(
            ToolPropertyType.Object,
            "Moves an existing file to a new location.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(MoveFileChange.Kind)] = KindSchema(WorkspaceChangeKind.MoveFile),

                [nameof(MoveFileChange.SourcePath)] = new(
                    ToolPropertyType.String,
                    "The current path of the file."),

                [nameof(MoveFileChange.DestinationPath)] = new(
                    ToolPropertyType.String,
                    "The destination path of the file.")
            },

            Required =
            [
                nameof(MoveFileChange.Kind),
                nameof(MoveFileChange.SourcePath),
                nameof(MoveFileChange.DestinationPath)
            ]
        };

    public static readonly ToolProperty CreateDirectoryChangeProp =
        new(
            ToolPropertyType.Object,
            "Creates a new directory.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),


                [nameof(CreateDirectoryChange.Kind)] = KindSchema(WorkspaceChangeKind.CreateDirectory),

                [nameof(CreateDirectoryChange.DirectoryPath)] = new(
                    ToolPropertyType.String,
                    "The path of the directory to create.")
            },

            Required =
            [
                nameof(CreateDirectoryChange.Kind),
                nameof(CreateDirectoryChange.DirectoryPath)
            ]
        };

    public static readonly ToolProperty DeleteDirectoryChangeProp =
        new(
            ToolPropertyType.Object,
            "Deletes an existing directory.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(DeleteDirectoryChange.Kind)] = KindSchema(WorkspaceChangeKind.DeleteDirectory),

                [nameof(DeleteDirectoryChange.DirectoryPath)] = new(
                    ToolPropertyType.String,
                    "The path of the directory to delete.")
            },

            Required =
            [
                nameof(DeleteDirectoryChange.Kind),
                nameof(DeleteDirectoryChange.DirectoryPath)
            ]
        };

    public static readonly ToolProperty RenameDirectoryChangeProp =
        new(
            ToolPropertyType.Object,
            "Renames an existing directory.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(RenameDirectoryChange.Kind)] = KindSchema(WorkspaceChangeKind.RenameDirectory),

                [nameof(RenameDirectoryChange.OldPath)] = new(
                    ToolPropertyType.String,
                    "The current path of the directory."),

                [nameof(RenameDirectoryChange.NewPath)] = new(
                    ToolPropertyType.String,
                    "The new path of the directory.")
            },

            Required =
            [
                nameof(RenameDirectoryChange.Kind),
                nameof(RenameDirectoryChange.OldPath),
                nameof(RenameDirectoryChange.NewPath)
            ]
        };

    public static readonly ToolProperty MoveDirectoryChangeProp =
        new(
            ToolPropertyType.Object,
            "Moves an existing directory to a new location.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                //[nameof(WorkspaceChange.Id)] = new(
                //    ToolPropertyType.String,
                //    "Unique identifier of the change."),

                [nameof(MoveDirectoryChange.Kind)] = KindSchema(WorkspaceChangeKind.MoveDirectory),

                [nameof(MoveDirectoryChange.SourcePath)] = new(
                    ToolPropertyType.String,
                    "The current path of the directory."),

                [nameof(MoveDirectoryChange.DestinationPath)] = new(
                    ToolPropertyType.String,
                    "The destination path of the directory.")
            },

            Required =
            [
                nameof(MoveDirectoryChange.Kind),
                nameof(MoveDirectoryChange.SourcePath),
                nameof(MoveDirectoryChange.DestinationPath)
            ]
        };


    public static readonly ToolProperty WorkspaceChangeProp =
        new(
            ToolPropertyType.Object,
            "Represents a single workspace modification.")
        {
            OneOf =
            [
                CreateFileChangeProp,
                EditFileChangeProp,
                DeleteFileChangeProp,
                RenameFileChangeProp,
                MoveFileChangeProp,
                CreateDirectoryChangeProp,
                DeleteDirectoryChangeProp,
                RenameDirectoryChangeProp,
                MoveDirectoryChangeProp
            ]
        };
    public static readonly ToolProperty WorkspaceChangeSetProp =
        new(
            ToolPropertyType.Array,
            "Ordered list of workspace modifications.")
        {
            Items = WorkspaceChangeProp
        };

}