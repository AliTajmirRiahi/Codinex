using System;
using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Bitap (Shift-And). Bit-parallel exact search: each pattern position is a bit in a machine word, and
/// one text character advances every bit position's match state in a single AND + shift operation.
/// </summary>
/// <remarks>
/// Complexity: preprocessing O(m + alphabet size), search O(n) with small constant factor (one word
/// operation per text character).
/// <para>
/// Hard limitation: the pattern must fit in a single 64-bit word, i.e. <see cref="MaxPatternLength"/>
/// (64) characters. Longer patterns are rejected explicitly rather than silently truncated or split.
/// </para>
/// </remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class BitapStringSearchAlgorithm : IStringSearchAlgorithm
{
    /// <summary>The pattern must be no longer than this many characters (bits in a ulong).</summary>
    public const int MaxPatternLength = 64;

    public SearchAlgorithmType Algorithm => SearchAlgorithmType.Bitap;

    public string Name => "Bitap (Shift-And)";

    public IReadOnlyList<SearchMatch> Search(string text, string pattern, StringSearchOptions options)
    {
        SearchOptionsHelper.ValidateTextAndPattern(text, pattern);
        SearchOptionsHelper.ValidateOptions(options);

        if (pattern.Length > MaxPatternLength)
        {
            throw new NotSupportedException(
                $"Bitap supports patterns up to {MaxPatternLength} characters; pattern length was {pattern.Length}.");
        }

        var matches = new List<SearchMatch>();

        if (pattern.Length > text.Length)
            return matches;

        var ignoreCase = SearchOptionsHelper.IsCaseInsensitive(options);
        var haystack = SearchOptionsHelper.Normalize(text, ignoreCase);
        var needle = SearchOptionsHelper.Normalize(pattern, ignoreCase);
        var wholeWord = SearchOptionsHelper.IsWholeWord(options);

        var m = needle.Length;
        var matchMask = 1UL << (m - 1);

        var charMasks = BuildCharMasks(needle);

        var state = 0UL;

        for (var i = 0; i < haystack.Length; i++)
        {
            charMasks.TryGetValue(haystack[i], out var mask);

            state = ((state << 1) | 1UL) & mask;

            if ((state & matchMask) == 0)
                continue;

            var start = i - m + 1;

            if (!wholeWord || SearchOptionsHelper.IsWholeWordMatch(text, start, m))
            {
                matches.Add(SearchMatchFactory.CreateExact(text, start, m, Algorithm, options));

                if (matches.Count >= options.MaxResults)
                    break;
            }
        }

        return matches;
    }

    /// <summary>For each character c, bit k is set iff pattern[k] == c; ANDing the shifted state with this mask keeps only the still-consistent match states.</summary>
    private static Dictionary<char, ulong> BuildCharMasks(char[] pattern)
    {
        var masks = new Dictionary<char, ulong>(pattern.Length);

        for (var i = 0; i < pattern.Length; i++)
        {
            masks.TryGetValue(pattern[i], out var mask);
            masks[pattern[i]] = mask | (1UL << i);
        }

        return masks;
    }
}
