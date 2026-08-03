namespace Codinex.Core.Models.WorkspaceChanges;
/// <summary>
/// Represents deleting a file.
/// </summary>
public sealed class DeleteFileChange : WorkspaceChange
{
    public string FilePath { get; set; }
    public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.DeleteFile;
}