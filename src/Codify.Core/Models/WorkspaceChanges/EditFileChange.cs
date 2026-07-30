using System.Collections.Generic;

namespace Codify.Core.Models.WorkspaceChanges;
/// <summary>
/// Represents all text modifications for a single file.
/// </summary>
public sealed class EditFileChange : WorkspaceChange
{
    public string FilePath { get; set; }

    public List<TextFileChange> TextChanges { get; set; } = [];
    public override WorkspaceChangeKind Kind { get; } = WorkspaceChangeKind.EditFile;
}