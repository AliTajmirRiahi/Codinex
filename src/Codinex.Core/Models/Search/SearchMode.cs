namespace Codinex.Core.Models.Search;

/// <summary>
/// Controls how a pattern is compared against source text.
/// </summary>
public enum SearchMode
{
    /// <summary>Ordinal, case-sensitive exact match. The default for source-code searching.</summary>
    Exact,

    /// <summary>Ordinal, case-insensitive exact match.</summary>
    CaseInsensitive,

    /// <summary>Explicit alias for <see cref="Exact"/>, kept for callers that reason in terms of <see cref="System.StringComparison"/>.</summary>
    Ordinal,

    /// <summary>Explicit alias for <see cref="CaseInsensitive"/>.</summary>
    OrdinalIgnoreCase,

    /// <summary>Exact match where the pattern must be bounded by non-word characters (or text edges).</summary>
    WholeWord,

    /// <summary>Multiple patterns are searched for in a single pass. Requires a multi-pattern-capable algorithm.</summary>
    MultiPattern,

    /// <summary>Approximate match using an edit-distance metric instead of exact comparison.</summary>
    Fuzzy
}
