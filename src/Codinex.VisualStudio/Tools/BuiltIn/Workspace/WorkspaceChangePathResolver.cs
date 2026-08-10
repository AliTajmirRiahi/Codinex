using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// Resolves the single path that identifies a <see cref="WorkspaceChange"/> for
/// display and per-file accept/reject matching. Shared by
/// <see cref="WorkspacePreviewService"/> (building the review payload) and
/// <see cref="ChangeSetCreatorTool"/> (matching the user's per-file decisions
/// back to the changes to apply), so the two never drift apart.
/// </summary>
public static class WorkspaceChangePathResolver
{
    public static string GetPath(WorkspaceChange change) => change switch
    {
        EditFileChange edit => edit.FilePath,
        CreateFileChange create => create.FilePath,
        DeleteFileChange delete => delete.FilePath,
        RenameFileChange rename => rename.FilePath,
        MoveFileChange move => move.SourcePath,
        CreateDirectoryChange createDirectory => createDirectory.DirectoryPath,
        DeleteDirectoryChange deleteDirectory => deleteDirectory.DirectoryPath,
        RenameDirectoryChange renameDirectory => renameDirectory.OldPath,
        MoveDirectoryChange moveDirectory => moveDirectory.SourcePath,
        _ => string.Empty
    };
}
