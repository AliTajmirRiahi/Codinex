using System.Collections.Generic;

namespace Codinex.Core.Models.Search;

/// <summary>
/// A request to locate one or more patterns inside a body of text.
/// </summary>
public sealed class SearchRequest
{
    /// <summary>The text to search within.</summary>
    public string Text { get; set; }

    /// <summary>The pattern to search for. Ignored when <see cref="Patterns"/> is set.</summary>
    public string Pattern { get; set; }

    /// <summary>
    /// Multiple patterns to search for in a single pass. When set, requires a multi-pattern-capable
    /// algorithm (e.g. Aho-Corasick) or <see cref="StringSearchOptions.Algorithm"/> = Auto.
    /// </summary>
    public IReadOnlyList<string> Patterns { get; set; }

    public StringSearchOptions Options { get; set; } = new();

    public bool IsMultiPattern => Patterns != null && Patterns.Count > 0;
}
