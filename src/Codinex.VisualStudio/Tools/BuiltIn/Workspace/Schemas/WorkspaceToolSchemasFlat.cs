using System;
using System.Collections.Generic;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace.Schemas;

internal static class WorkspaceToolSchemasFlat
{
    private static ToolProperty KindSchema()
    {
        return new ToolProperty(
            ToolPropertyType.String,
            "The type of modification to perform.")
        {
            Enum =
            [
                nameof(WorkspaceChangeKind.CreateFile),
                nameof(WorkspaceChangeKind.EditFile),
                nameof(WorkspaceChangeKind.DeleteFile),
                nameof(WorkspaceChangeKind.RenameFile),
                nameof(WorkspaceChangeKind.MoveFile),
                nameof(WorkspaceChangeKind.CreateDirectory),
                nameof(WorkspaceChangeKind.DeleteDirectory),
                nameof(WorkspaceChangeKind.RenameDirectory),
                nameof(WorkspaceChangeKind.MoveDirectory)
            ]
        };
    }

    public static readonly ToolProperty TextFileChangeProp =
        new(
            ToolPropertyType.Object,
            "Represents a single text modification.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
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
            ]
        };

    // Flat, non-union representation of a single workspace change.
    // Only "Kind" is required; which of the remaining fields are meaningful
    // depends on the value of Kind (documented per-field below).
    public static readonly ToolProperty WorkspaceChangeProp =
        new(
            ToolPropertyType.Object,
            "Represents a single workspace modification. Only populate the fields relevant to the chosen Kind; omit or leave the rest empty.")
        {
            Properties = new Dictionary<string, ToolProperty>
            {
                [nameof(CreateFileChange.Kind)] = KindSchema(),

                [nameof(CreateFileChange.FilePath)] = new(
                    ToolPropertyType.String,
                    "The path of the file. Required for: CreateFile, EditFile, DeleteFile, RenameFile."),

                [nameof(CreateFileChange.Content)] = new(
                    ToolPropertyType.String,
                    "Complete file contents. Use an empty string for an empty file."),

                [nameof(EditFileChange.TextChanges)] = new(
                    ToolPropertyType.Array,
                    "Ordered list of text modifications. Required for: EditFile.")
                {
                    Items = TextFileChangeProp
                },

                [nameof(RenameFileChange.NewFileName)] = new(
                    ToolPropertyType.String,
                    "The new file name without changing its directory. Required for: RenameFile."),

                [nameof(MoveFileChange.SourcePath)] = new(
                    ToolPropertyType.String,
                    "The current path of the file or directory. Required for: MoveFile, MoveDirectory."),

                [nameof(MoveFileChange.DestinationPath)] = new(
                    ToolPropertyType.String,
                    "The destination path of the file or directory. Required for: MoveFile, MoveDirectory."),

                [nameof(CreateDirectoryChange.DirectoryPath)] = new(
                    ToolPropertyType.String,
                    "The path of the directory. Required for: CreateDirectory, DeleteDirectory."),

                [nameof(RenameDirectoryChange.OldPath)] = new(
                    ToolPropertyType.String,
                    "The current path of the directory. Required for: RenameDirectory."),

                [nameof(RenameDirectoryChange.NewPath)] = new(
                    ToolPropertyType.String,
                    "The new path of the directory. Required for: RenameDirectory.")
            },

            Required =
            [
                nameof(CreateFileChange.Kind),
                nameof(CreateFileChange.FilePath),
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