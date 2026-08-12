using System;
using System.Collections.Generic;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search;

/// <summary>
/// Composes the registered <see cref="IStringSearchAlgorithm"/> strategies behind a single entry
/// point: resolves <see cref="SearchAlgorithmType.Auto"/> via the injected
/// <see cref="ISearchAlgorithmSelector"/>, runs the chosen algorithm, and fills in line/column
/// information for every match. It never decides which candidate is "the" match — that is left to the
/// caller (e.g. a TextFileChange validator).
/// </summary>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class CodeSearchEngine(
    IEnumerable<IStringSearchAlgorithm> algorithms,
    ISearchAlgorithmSelector selector)
    : ICodeSearchEngine
{
    private readonly IReadOnlyList<IStringSearchAlgorithm> _algorithms = algorithms?.ToArray() ?? throw new ArgumentNullException(nameof(algorithms));
    private readonly ISearchAlgorithmSelector _selector = selector ?? throw new ArgumentNullException(nameof(selector));

    public CodeSearchResult Search(SearchRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Text == null)
            throw new ArgumentException("Request.Text must not be null.", nameof(request));

        var options = request.Options ?? new StringSearchOptions();

        var algorithm = ResolveAlgorithm(request, options);

        var matches = request.IsMultiPattern
            ? RunMultiPattern(algorithm, request, options)
            : RunSinglePattern(algorithm, request, options);

        PopulateLocations(request.Text, matches);

        var filtered = matches
            .Where(m => m.Score >= options.MinimumScore)
            .Take(options.MaxResults)
            .ToArray();

        return new CodeSearchResult
        {
            Matches = filtered,
            AlgorithmUsed = algorithm.Algorithm
        };
    }

    private IStringSearchAlgorithm ResolveAlgorithm(SearchRequest request, StringSearchOptions options)
    {
        if (options.Algorithm != SearchAlgorithmType.Auto)
        {
            var explicitAlgorithm = _algorithms.FirstOrDefault(a => a.Algorithm == options.Algorithm);

            if (explicitAlgorithm == null)
            {
                throw new InvalidOperationException(
                    $"No IStringSearchAlgorithm is registered for {options.Algorithm}.");
            }

            return explicitAlgorithm;
        }

        return _selector.Select(request, _algorithms);
    }

    private static IReadOnlyList<SearchMatch> RunSinglePattern(
        IStringSearchAlgorithm algorithm,
        SearchRequest request,
        StringSearchOptions options)
    {
        if (request.Pattern == null)
            throw new ArgumentException("Request.Pattern must not be null for a single-pattern search.", nameof(request));

        return algorithm.Search(request.Text, request.Pattern, options);
    }

    private static IReadOnlyList<SearchMatch> RunMultiPattern(
        IStringSearchAlgorithm algorithm,
        SearchRequest request,
        StringSearchOptions options)
    {
        if (algorithm is not IMultiPatternStringSearchAlgorithm multiPatternAlgorithm)
        {
            throw new InvalidOperationException(
                $"Algorithm {algorithm.Algorithm} does not support multi-pattern search.");
        }

        return multiPatternAlgorithm.SearchMultiple(request.Text, request.Patterns, options);
    }

    private static void PopulateLocations(string text, IReadOnlyList<SearchMatch> matches)
    {
        if (matches.Count == 0)
            return;

        var lineIndex = new SearchLineIndex(text);

        foreach (var match in matches)
        {
            var (startLine, startColumn) = lineIndex.GetLineColumn(match.Range.Start);
            var (endLine, endColumn) = lineIndex.GetLineColumn(match.Range.Start + match.Range.Length);

            match.Range.StartLine = startLine;
            match.Range.StartColumn = startColumn;
            match.Range.EndLine = endLine;
            match.Range.EndColumn = endColumn;
        }
    }
}
