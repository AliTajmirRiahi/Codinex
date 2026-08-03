namespace Codify.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents the type of workspace change.
/// </summary>
public enum WorkspaceChangeKind
{
    EditFile,
    CreateFile,
    DeleteFile,
    RenameFile,
    MoveFile,

    CreateDirectory,
    DeleteDirectory,
    RenameDirectory,
    MoveDirectory
}