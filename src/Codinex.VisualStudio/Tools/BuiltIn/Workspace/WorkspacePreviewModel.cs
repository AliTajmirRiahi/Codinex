using System;
using System.Collections.Generic;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// A workspace change set rendered for user review (diff, file tree, summary).
/// Posted to the Code Changes review WebView as the CHANGESET_SHOW payload.
/// </summary>
public sealed class WorkspacePreviewModel
{
    public Guid Id { get; set; }

    public string Summary { get; set; }

    public List<ChangesetFileDiff> Files { get; set; } = [];
}

/// <summary>
/// The before/after content for a single file within a reviewed change set.
/// </summary>
public sealed class ChangesetFileDiff
{
    public string FilePath { get; set; }

    /// <summary>
    /// One of "EditFile", "CreateFile", "DeleteFile", "RenameFile", "MoveFile",
    /// "CreateDirectory", "DeleteDirectory", "RenameDirectory", "MoveDirectory".
    /// </summary>
    public string Operation { get; set; }

    public string OriginalText { get; set; }

    public string ModifiedText { get; set; }

    /// <summary>
    /// Set when one of this file's edits couldn't be located in the current file content while
    /// building the preview (e.g. the file changed since the AI proposed the edit). The diff shown
    /// stops at the failure point, so it may look like fewer/no changes even though more were
    /// proposed — this is surfaced instead of silently rendering as if nothing changed.
    /// </summary>
    public string PreviewWarning { get; set; }
}
