using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Boyer-Moore-Horspool: a simpler, practical variant of Boyer-Moore that uses only a single
/// bad-character shift table, keyed on the text character aligned with the pattern's last position.
/// Cheaper to preprocess than full Boyer-Moore and, for typical source-code alphabets and pattern
/// lengths, performs comparably in practice — the recommended default for normal exact source-code
/// searching.
/// </summary>
/// <remarks>Complexity: preprocessing O(m + alphabet size), search O(n*m) worst case, sub-linear average case.</remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class BoyerMooreHorspoolStringSearchAlgorithm : IStringSearchAlgorithm
{
    public SearchAlgorithmType Algorithm => SearchAlgorithmType.BoyerMooreHorspool;

    public string Name => "Boyer-Moore-Horspool";

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

        var shift = BuildShiftTable(needle);

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

                // Advance by one so overlapping occurrences aren't skipped, rather than by shift[last char].
                s += 1;
            }
            else
            {
                var lastCharInWindow = haystack[s + m - 1];

                s += shift.TryGetValue(lastCharInWindow, out var value) ? value : m;
            }
        }

        return matches;
    }

    /// <summary>shift[c] = distance to move the pattern so its rightmost occurrence of c aligns with the text's c, or m if c doesn't occur in the pattern (excluding the pattern's last character).</summary>
    private static Dictionary<char, int> BuildShiftTable(char[] pattern)
    {
        var m = pattern.Length;
        var table = new Dictionary<char, int>(m);

        for (var i = 0; i < m - 1; i++)
            table[pattern[i]] = m - 1 - i;

        return table;
    }
}
