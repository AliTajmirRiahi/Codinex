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
}
