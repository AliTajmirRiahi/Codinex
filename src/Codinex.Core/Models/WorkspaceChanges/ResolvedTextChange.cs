using System;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// A single TextFileChange after Find + Validate + Plan: the exact range it occupies in
/// the current document, and the text on either side of applying it.
/// </summary>
public sealed class ResolvedTextChange
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    /// <summary>The Search text that uniquely resolved this change.</summary>
    public string Search { get; set; }

    public string Replace { get; set; }

    /// <summary>Exact location in the current document.</summary>
    public TextRange Range { get; set; }

    /// <summary>The exact text currently occupying the resolved range.</summary>
    public string OriginalText { get; set; }

    /// <summary>The exact text that will occupy the range after applying the change.</summary>
    public string ResultText { get; set; }
}
