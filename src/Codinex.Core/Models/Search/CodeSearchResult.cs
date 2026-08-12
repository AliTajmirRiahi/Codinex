using System.Collections.Generic;

namespace Codinex.Core.Models.Search;

/// <summary>
/// The outcome of an <c>ICodeSearchEngine</c> search: candidate locations for the caller (typically a
/// validator) to disambiguate. The engine never decides which candidate is correct.
/// </summary>
public sealed class CodeSearchResult
{
    public IReadOnlyList<SearchMatch> Matches { get; set; } = System.Array.Empty<SearchMatch>();

    /// <summary>The algorithm actually used to produce <see cref="Matches"/> (resolved from Auto, if requested).</summary>
    public SearchAlgorithmType AlgorithmUsed { get; set; }
}
