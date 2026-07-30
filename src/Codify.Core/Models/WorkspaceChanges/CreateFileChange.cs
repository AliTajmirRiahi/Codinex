namespace Codify.Core.Models.WorkspaceChanges;
/// <summary>
/// Represents creating a new file.
/// </summary>
public sealed class CreateFileChange : WorkspaceChange
{
    public string FilePath { get; set; }

    public string Content { get; set; }
    public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.CreateFile;
}