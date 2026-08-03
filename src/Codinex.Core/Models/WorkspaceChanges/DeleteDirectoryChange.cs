namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents deleting a directory.
/// </summary>
public sealed class DeleteDirectoryChange : WorkspaceChange
{
    public string DirectoryPath { get; set; }
    public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.DeleteDirectory;
}