namespace Codinex.Core.Models.WorkspaceChanges;
/// <summary>
/// Represents creating a directory.
/// </summary>
public sealed class CreateDirectoryChange : WorkspaceChange
{
    public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.CreateDirectory;
    public string DirectoryPath { get; set; }
}