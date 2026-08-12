using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Models.Search;

/// <summary>
/// A single candidate location produced by a string-search algorithm.
/// Deliberately independent of the search engine and algorithm types so it can be consumed by
/// <c>TextFileChange</c> resolution, validators, the diff viewer, and the applier without a dependency
/// back onto the search engine.
/// </summary>
public sealed class SearchMatch
{
    /// <summary>Exact location of the match within the searched text.</summary>
    public TextRange Range { get; set; }

    public int StartIndex => Range?.Start ?? 0;

    public int EndIndex => (Range?.Start ?? 0) + (Range?.Length ?? 0);

    public int StartLine => Range?.StartLine ?? 0;

    public int StartColumn => Range?.StartColumn ?? 0;

    public int EndLine => Range?.EndLine ?? 0;

    public int EndColumn => Range?.EndColumn ?? 0;

    /// <summary>Normalized match quality: 1.0 for exact matches, lower for fuzzy matches.</summary>
    public double Score { get; set; }

    /// <summary>The algorithm that produced this match.</summary>
    public SearchAlgorithmType Algorithm { get; set; }

    public MatchType MatchType { get; set; }

    /// <summary>The literal text found at <see cref="Range"/>.</summary>
    public string MatchedText { get; set; }

    /// <summary>
    /// For multi-pattern searches (e.g. Aho-Corasick), the pattern that produced this match and its
    /// index in the request's pattern list. Unused for single-pattern searches.
    /// </summary>
    public string MatchedPattern { get; set; }

    public int PatternIndex { get; set; } = -1;
}
