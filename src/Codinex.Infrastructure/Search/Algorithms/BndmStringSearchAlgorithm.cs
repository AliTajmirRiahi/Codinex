using System;
using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Backward Nondeterministic DAWG Matching. Simulates, bit-parallel, the nondeterministic automaton
/// that recognizes factors of the reversed pattern while scanning each m-length text window from right
/// to left. As long as the active-state bitmask stays non-zero, the characters read so far form a
/// factor of the pattern; a set bit at the leftmost automaton position after scanning the whole window
/// means the window equals the pattern. The last position (from the right) at which a partial match was
/// still alive gives a safe shift for the next window — this can be shorter than the pattern length,
/// which is how overlapping occurrences are still found.
/// </summary>
/// <remarks>
/// Complexity: preprocessing O(m + alphabet size), search O(n*m) worst case but typically sub-linear
/// (often faster in practice than Horspool for longer patterns) since a window can be abandoned as
/// soon as the bitmask goes to zero.
/// <para>
/// Hard limitation: like Bitap, the pattern must fit in a single 64-bit word
/// (see <see cref="MaxPatternLength"/>).
/// </para>
/// </remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class BndmStringSearchAlgorithm : IStringSearchAlgorithm
{
    /// <summary>The pattern must be no longer than this many characters (bits in a ulong).</summary>
    public const int MaxPatternLength = 64;

    public SearchAlgorithmType Algorithm => SearchAlgorithmType.Bndm;

    public string Name => "BNDM";

    public IReadOnlyList<SearchMatch> Search(string text, string pattern, StringSearchOptions options)
    {
        SearchOptionsHelper.ValidateTextAndPattern(text, pattern);
        SearchOptionsHelper.ValidateOptions(options);

        if (pattern.Length > MaxPatternLength)
        {
            throw new NotSupportedException(
                $"BNDM supports patterns up to {MaxPatternLength} characters; pattern length was {pattern.Length}.");
        }

        var matches = new List<SearchMatch>();

        if (pattern.Length > text.Length)
            return matches;

        var ignoreCase = SearchOptionsHelper.IsCaseInsensitive(options);
        var haystack = SearchOptionsHelper.Normalize(text, ignoreCase);
        var needle = SearchOptionsHelper.Normalize(pattern, ignoreCase);
        var wholeWord = SearchOptionsHelper.IsWholeWord(options);

        var m = needle.Length;
        var n = haystack.Length;

        var masks = BuildReversePatternMasks(needle);

        var j = 0;

        while (j <= n - m)
        {
            var i = m - 1;
            var last = m;
            var activeStates = ulong.MaxValue;

            while (activeStates != 0)
            {
                masks.TryGetValue(haystack[j + i], out var charMask);
                activeStates &= charMask;

                if (activeStates != 0)
                {
                    if (i == 0)
                    {
                        if (!wholeWord || SearchOptionsHelper.IsWholeWordMatch(text, j, m))
                        {
                            matches.Add(SearchMatchFactory.CreateExact(text, j, m, Algorithm, options));

                            if (matches.Count >= options.MaxResults)
                                return matches;
                        }

                        break;
                    }

                    last = i;
                }

                i--;
                activeStates <<= 1;
            }

            j += last;
        }

        return matches;
    }

    /// <summary>mask[c] has bit i set when the reversed pattern's i-th character (i.e. pattern[m-1-i]) equals c.</summary>
    private static Dictionary<char, ulong> BuildReversePatternMasks(char[] pattern)
    {
        var m = pattern.Length;
        var masks = new Dictionary<char, ulong>(m);

        foreach (var c in pattern)
            masks[c] = 0UL;

        for (var i = 0; i < m; i++)
        {
            var c = pattern[m - 1 - i];
            masks[c] |= 1UL << i;
        }

        return masks;
    }
}
