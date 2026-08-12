using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Knuth-Morris-Pratt: uses the pattern's failure function (longest proper prefix that is also a
/// suffix, for every prefix of the pattern) to avoid re-scanning text characters after a mismatch.
/// Predictable linear-time behavior makes it a good default for large files.
/// </summary>
/// <remarks>Complexity: preprocessing O(m), search O(n). No backtracking on the text pointer.</remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class KmpStringSearchAlgorithm : IStringSearchAlgorithm
{
    public SearchAlgorithmType Algorithm => SearchAlgorithmType.Kmp;

    public string Name => "Knuth-Morris-Pratt";

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

        var failure = BuildFailureTable(needle);

        var i = 0;
        var j = 0;

        while (i < haystack.Length)
        {
            if (haystack[i] == needle[j])
            {
                i++;
                j++;

                if (j == needle.Length)
                {
                    var start = i - j;

                    if (!wholeWord || SearchOptionsHelper.IsWholeWordMatch(text, start, needle.Length))
                    {
                        matches.Add(SearchMatchFactory.CreateExact(text, start, needle.Length, Algorithm, options));

                        if (matches.Count >= options.MaxResults)
                            return matches;
                    }

                    // Resume from the failure table (not j = 0) so overlapping occurrences are found too.
                    j = failure[j - 1];
                }

                continue;
            }

            if (j != 0)
            {
                j = failure[j - 1];
            }
            else
            {
                i++;
            }
        }

        return matches;
    }

    /// <summary>
    /// failure[k] = length of the longest proper prefix of pattern[0..k] that is also a suffix of it.
    /// </summary>
    private static int[] BuildFailureTable(char[] pattern)
    {
        var failure = new int[pattern.Length];
        var length = 0;
        var i = 1;

        while (i < pattern.Length)
        {
            if (pattern[i] == pattern[length])
            {
                length++;
                failure[i] = length;
                i++;
            }
            else if (length != 0)
            {
                length = failure[length - 1];
            }
            else
            {
                failure[i] = 0;
                i++;
            }
        }

        return failure;
    }
}
