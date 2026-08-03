namespace Codinex.Core.Models.WorkspaceChanges
{
    /// <summary>
    /// Represents renaming a directory.
    /// </summary>
    public sealed class RenameDirectoryChange : WorkspaceChange
    {
        public string OldPath { get; set; }

        public string NewPath { get; set; }

        public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.RenameDirectory;
    }
}