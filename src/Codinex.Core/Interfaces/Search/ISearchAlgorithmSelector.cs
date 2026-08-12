using System.Collections.Generic;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Core.Interfaces.Search;

/// <summary>
/// Chooses which algorithm to run for a <see cref="SearchRequest"/> whose
/// <see cref="StringSearchOptions.Algorithm"/> is <see cref="SearchAlgorithmType.Auto"/>.
/// Kept as its own abstraction so the heuristic can be replaced/tuned (e.g. from benchmark results)
/// without touching the engine.
/// </summary>
public interface ISearchAlgorithmSelector
{
    /// <summary>
    /// Picks an algorithm from <paramref name="availableAlgorithms"/> best suited to <paramref name="request"/>.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// No registered algorithm can satisfy the request (e.g. multi-pattern requested but no
    /// <see cref="IMultiPatternStringSearchAlgorithm"/> is registered).
    /// </exception>
    IStringSearchAlgorithm Select(
        SearchRequest request,
        IReadOnlyList<IStringSearchAlgorithm> availableAlgorithms);
}
