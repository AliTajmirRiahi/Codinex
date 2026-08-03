namespace Codify.Core.Models.WorkspaceChanges
{
    /// <summary>
    /// Represents moving a directory.
    /// </summary>
    public sealed class MoveDirectoryChange : WorkspaceChange
    {
        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }
        public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.MoveDirectory;
    }
}