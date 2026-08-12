using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Full Boyer-Moore: combines the bad-character rule (skip past a mismatched text character to its
/// last occurrence in the pattern) with the good-suffix rule (reuse the already-matched suffix to
/// skip further than the bad-character rule alone would allow). At each mismatch the larger of the
/// two shifts is taken.
/// </summary>
/// <remarks>
/// Complexity: preprocessing O(m + alphabet size), search O(n/m) best case, O(n*m) worst case
/// (mitigated in practice by the good-suffix rule, which guarantees no text character is examined
/// more than a constant number of times when a Boyer-Moore-style skip is used).
/// </remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class BoyerMooreStringSearchAlgorithm : IStringSearchAlgorithm
{
    public SearchAlgorithmType Algorithm => SearchAlgorithmType.BoyerMoore;

    public string Name => "Boyer-Moore";

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

        var m = needle.Length;
        var n = haystack.Length;

        var badChar = BuildBadCharTable(needle);
        var goodSuffixShift = BuildGoodSuffixTable(needle);

        var s = 0;

        while (s <= n - m)
        {
            var j = m - 1;

            while (j >= 0 && needle[j] == haystack[s + j])
                j--;

            if (j < 0)
            {
                if (!wholeWord || SearchOptionsHelper.IsWholeWordMatch(text, s, m))
                {
                    matches.Add(SearchMatchFactory.CreateExact(text, s, m, Algorithm, options));

                    if (matches.Count >= options.MaxResults)
                        break;
                }

                s += goodSuffixShift[0];
            }
            else
            {
                var badCharShift = j - LastOccurrence(badChar, haystack[s + j]);
                var shift = System.Math.Max(1, System.Math.Max(badCharShift, goodSuffixShift[j + 1]));

                s += shift;
            }
        }

        return matches;
    }

    private static Dictionary<char, int> BuildBadCharTable(char[] pattern)
    {
        var table = new Dictionary<char, int>(pattern.Length);

        for (var i = 0; i < pattern.Length; i++)
            table[pattern[i]] = i;

        return table;
    }

    private static int LastOccurrence(Dictionary<char, int> badChar, char c)
    {
        return badChar.TryGetValue(c, out var index) ? index : -1;
    }

    /// <summary>
    /// Standard good-suffix preprocessing: for each mismatch position, how far the pattern can slide
    /// while keeping the already-matched suffix (or a matching prefix of it) aligned.
    /// </summary>
    private static int[] BuildGoodSuffixTable(char[] pattern)
    {
        var m = pattern.Length;
        var shift = new int[m + 1];
        var borderPos = new int[m + 1];

        var i = m;
        var j = m + 1;
        borderPos[i] = j;

        while (i > 0)
        {
            while (j <= m && pattern[i - 1] != pattern[j - 1])
            {
                if (shift[j] == 0)
                    shift[j] = j - i;

                j = borderPos[j];
            }

            i--;
            j--;
            borderPos[i] = j;
        }

        j = borderPos[0];

        for (i = 0; i <= m; i++)
        {
            if (shift[i] == 0)
                shift[i] = j;

            if (i == j)
                j = borderPos[j];
        }

        return shift;
    }
}
