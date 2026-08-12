using System.Collections.Generic;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search;

/// <summary>
/// Default <see cref="SearchAlgorithmType.Auto"/> heuristic. These thresholds are starting points, not
/// hard-coded assumptions — they are meant to be refined from real benchmark results, which is why the
/// selector is its own replaceable service rather than logic baked into the engine.
/// </summary>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class SearchAlgorithmSelector : ISearchAlgorithmSelector
{
    /// <summary>Patterns shorter than this fall back to Naive: preprocessing overhead isn't worth it.</summary>
    public const int SmallPatternLength = 4;

    /// <summary>Texts larger than this favor algorithms with guaranteed linear worst-case behavior.</summary>
    public const int LargeTextLength = 100_000;

    public IStringSearchAlgorithm Select(SearchRequest request, IReadOnlyList<IStringSearchAlgorithm> availableAlgorithms)
    {
        var options = request.Options ?? new StringSearchOptions();

        if (request.IsMultiPattern || options.ComparisonMode == SearchMode.MultiPattern)
            return RequireAlgorithm<IMultiPatternStringSearchAlgorithm>(availableAlgorithms, "multi-pattern");

        if (options.EnableFuzzy || options.ComparisonMode == SearchMode.Fuzzy)
            return RequireAlgorithm<IFuzzyStringSearchAlgorithm>(availableAlgorithms, "fuzzy");

        var patternLength = request.Pattern?.Length ?? 0;
        var textLength = request.Text?.Length ?? 0;

        if (patternLength > 0 && patternLength < SmallPatternLength)
            return FindByType(availableAlgorithms, SearchAlgorithmType.Naive) ?? FirstExact(availableAlgorithms);

        if (textLength > LargeTextLength)
        {
            return FindByType(availableAlgorithms, SearchAlgorithmType.Kmp)
                ?? FindByType(availableAlgorithms, SearchAlgorithmType.TwoWay)
                ?? FirstExact(availableAlgorithms);
        }

        return FindByType(availableAlgorithms, SearchAlgorithmType.BoyerMooreHorspool)
            ?? FirstExact(availableAlgorithms);
    }

    private static IStringSearchAlgorithm FindByType(IReadOnlyList<IStringSearchAlgorithm> algorithms, SearchAlgorithmType type)
    {
        return algorithms.FirstOrDefault(a => a.Algorithm == type);
    }

    private static IStringSearchAlgorithm FirstExact(IReadOnlyList<IStringSearchAlgorithm> algorithms)
    {
        var fallback = algorithms.FirstOrDefault(a =>
            a is not IFuzzyStringSearchAlgorithm && a is not IMultiPatternStringSearchAlgorithm);

        if (fallback == null)
        {
            throw new System.InvalidOperationException(
                "No exact-match IStringSearchAlgorithm is registered.");
        }

        return fallback;
    }

    private static T RequireAlgorithm<T>(IReadOnlyList<IStringSearchAlgorithm> algorithms, string capability)
        where T : IStringSearchAlgorithm
    {
        var match = algorithms.OfType<T>().FirstOrDefault();

        if (match == null)
        {
            throw new System.InvalidOperationException(
                $"The request requires a {capability}-capable algorithm, but none is registered.");
        }

        return match;
    }
}
