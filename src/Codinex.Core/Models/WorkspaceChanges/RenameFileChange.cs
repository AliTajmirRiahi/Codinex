namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents renaming a file.
/// </summary>
public sealed class RenameFileChange : WorkspaceChange
{
    public string FilePath { get; set; }

    public string NewFileName { get; set; }

    public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.RenameFile;
}