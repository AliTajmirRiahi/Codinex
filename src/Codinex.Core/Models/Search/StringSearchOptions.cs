namespace Codinex.Core.Models.Search;

/// <summary>
/// Configures how a search request is executed.
/// </summary>
public sealed class StringSearchOptions
{
    /// <summary>
    /// The algorithm to use. Defaults to <see cref="SearchAlgorithmType.Auto"/>, which lets the
    /// registered <c>ISearchAlgorithmSelector</c> pick an algorithm based on the request shape.
    /// </summary>
    public SearchAlgorithmType Algorithm { get; set; } = SearchAlgorithmType.Auto;

    /// <summary>
    /// How the pattern is compared against the text. Source-code searching should use
    /// <see cref="SearchMode.Exact"/> or <see cref="SearchMode.CaseInsensitive"/> (both ordinal) rather
    /// than culture-sensitive comparison.
    /// </summary>
    public SearchMode ComparisonMode { get; set; } = SearchMode.Exact;

    /// <summary>
    /// When true, a match is only accepted if it is bounded by non-word characters (or text edges).
    /// </summary>
    public bool WholeWord { get; set; }

    /// <summary>
    /// Upper bound on the number of matches returned. Callers must never assume the first match is
    /// the correct one; multiple candidates are expected and left for the caller/validator to disambiguate.
    /// </summary>
    public int MaxResults { get; set; } = 100;

    /// <summary>
    /// Matches scoring below this threshold (0.0 - 1.0) are discarded. Exact matches always score 1.0.
    /// </summary>
    public double MinimumScore { get; set; }

    /// <summary>
    /// Enables fuzzy matching. Only honored when <see cref="Algorithm"/> is <see cref="SearchAlgorithmType.Fuzzy"/>
    /// or <see cref="SearchAlgorithmType.Auto"/> with <see cref="ComparisonMode"/> set to <see cref="SearchMode.Fuzzy"/>.
    /// </summary>
    public bool EnableFuzzy { get; set; }

    /// <summary>
    /// Maximum Levenshtein edit distance accepted by fuzzy matching. Ignored by exact algorithms.
    /// </summary>
    public int MaxEditDistance { get; set; } = 2;
}
