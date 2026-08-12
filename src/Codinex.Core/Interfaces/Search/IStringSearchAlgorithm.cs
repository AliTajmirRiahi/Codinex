using System.Collections.Generic;
using Codinex.Core.Models.Search;

namespace Codinex.Core.Interfaces.Search;

/// <summary>
/// A single string-search algorithm strategy. Implementations must be independent of any consumer
/// (AI providers, UI, Mission Engine) and stateless/thread-safe, since they are registered as singletons
/// and shared across concurrent searches.
/// </summary>
public interface IStringSearchAlgorithm
{
    /// <summary>The algorithm type this implementation provides, used for selection and result tagging.</summary>
    SearchAlgorithmType Algorithm { get; }

    /// <summary>Human-readable name, useful for diagnostics/benchmark output.</summary>
    string Name { get; }

    /// <summary>
    /// Finds every occurrence of <paramref name="pattern"/> in <paramref name="text"/>.
    /// Matches are returned in ascending order of <see cref="SearchMatch.StartIndex"/>.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">text or pattern is null.</exception>
    IReadOnlyList<SearchMatch> Search(
        string text,
        string pattern,
        StringSearchOptions options);
}
