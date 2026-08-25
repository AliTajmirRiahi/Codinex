using System;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.Workspace
{
    /// <summary>
    /// Watches the workspace for physical file additions, removals, and edits (post-save),
    /// so consumers can keep file-based reference lists in sync without re-scanning the solution.
    /// </summary>
    public interface IWorkspaceFileWatcher
    {
        event EventHandler<WorkspaceFileChangedEventArgs> FileAdded;
        event EventHandler<WorkspaceFileChangedEventArgs> FileRemoved;
        event EventHandler<WorkspaceFileChangedEventArgs> FileChanged;
    }

}
