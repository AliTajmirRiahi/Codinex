using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Brute-force substring search. Used as the correctness baseline every other algorithm is verified
/// against, and as the practical choice for very small patterns/texts where preprocessing overhead
/// outweighs any asymptotic gain.
/// </summary>
/// <remarks>
/// Complexity: preprocessing O(1), search O(n*m) worst case, O(n) best/average case for typical
/// source-code text.
/// </remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class NaiveStringSearchAlgorithm : IStringSearchAlgorithm
{
    public SearchAlgorithmType Algorithm => SearchAlgorithmType.Naive;

    public string Name => "Naive";

    public IReadOnlyList<SearchMatch> Search(string text, string pattern, StringSearchOptions options)
    {
        SearchOptionsHelper.ValidateTextAndPattern(text, pattern);
        SearchOptionsHelper.ValidateOptions(options);

        var matches = new List<SearchMatch>();

        if (pattern.Length > text.Length)
            return matches;

        var ignoreCase = SearchOptionsHelper.IsCaseInsensitive(options);
        var haystack = SearchOptionsHelper.Normalize(text, ignoreCase);
        var needle = SearchOptionsHelper.Normalize(pattern, ignoreCase);
        var wholeWord = SearchOptionsHelper.IsWholeWord(options);

        var lastStart = haystack.Length - needle.Length;

        for (var i = 0; i <= lastStart; i++)
        {
            var j = 0;

            while (j < needle.Length && haystack[i + j] == needle[j])
                j++;

            if (j != needle.Length)
                continue;

            if (wholeWord && !SearchOptionsHelper.IsWholeWordMatch(text, i, needle.Length))
                continue;

            matches.Add(SearchMatchFactory.CreateExact(text, i, needle.Length, Algorithm, options));

            if (matches.Count >= options.MaxResults)
                break;
        }

        return matches;
    }
}
