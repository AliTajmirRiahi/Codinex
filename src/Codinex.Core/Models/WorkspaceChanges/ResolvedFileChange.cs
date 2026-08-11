using System.Collections.Generic;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// A single EditFileChange after Find + Validate + Plan.
/// </summary>
public sealed class ResolvedFileChange
{
    public string FilePath { get; set; }

    public IReadOnlyList<ResolvedTextChange> TextChanges { get; set; } = [];
}
