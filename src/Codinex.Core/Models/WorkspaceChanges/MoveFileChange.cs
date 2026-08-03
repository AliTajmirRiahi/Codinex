namespace Codify.Core.Models.WorkspaceChanges;
/// <summary>
/// Represents moving a file.
/// </summary>
public sealed class MoveFileChange : WorkspaceChange
{
    public string SourcePath { get; set; }

    public string DestinationPath { get; set; }
    public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.MoveFile;
}