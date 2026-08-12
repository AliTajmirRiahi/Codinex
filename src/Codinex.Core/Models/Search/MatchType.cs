namespace Codinex.Core.Models.Search;

/// <summary>
/// Classifies how closely a <see cref="SearchMatch"/> corresponds to the requested pattern.
/// </summary>
public enum MatchType
{
    /// <summary>Ordinal, character-for-character match.</summary>
    Exact,

    /// <summary>Ordinal match ignoring case.</summary>
    CaseInsensitive,

    /// <summary>Exact match additionally bounded by word edges.</summary>
    WholeWord,

    /// <summary>Approximate match produced by an edit-distance algorithm.</summary>
    Fuzzy
}
