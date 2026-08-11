namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// The exact location a resolved text change occupies in a document.
/// </summary>
public sealed class TextRange
{
    public int Start { get; set; }

    public int Length { get; set; }

    public int StartLine { get; set; }

    public int StartColumn { get; set; }

    public int EndLine { get; set; }

    public int EndColumn { get; set; }
}
