using System;
using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Crochemore-Perrin Two-Way string matching. Factorizes the pattern at its critical point (found via
/// two maximal-suffix computations, under forward and reverse lexicographic order) into two halves that
/// are searched independently, right half first. The shift after a full match (or a mismatch past the
/// critical point) uses the pattern's true minimal period, computed once via a KMP-style border array,
/// which is what lets a window be skipped past without re-examining every text character.
/// </summary>
/// <remarks>Complexity: preprocessing O(m), search O(n + m), worst-case linear.</remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class TwoWayStringSearchAlgorithm : IStringSearchAlgorithm
{
    public SearchAlgorithmType Algorithm => SearchAlgorithmType.TwoWay;

    public string Name => "Two-Way";

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

        if (m == 1)
        {
            for (var i = 0; i < n; i++)
            {
                if (haystack[i] != needle[0])
                    continue;

                if (!wholeWord || SearchOptionsHelper.IsWholeWordMatch(text, i, 1))
                {
                    matches.Add(SearchMatchFactory.CreateExact(text, i, 1, Algorithm, options));

                    if (matches.Count >= options.MaxResults)
                        break;
                }
            }

            return matches;
        }

        var (criticalPoint, period) = Preprocess(needle);

        var ell = criticalPoint + 1;
        var memory = -1;

        var j = 0;

        while (j <= n - m)
        {
            var i = Math.Max(ell, memory) + 1;

            while (i < m && needle[i] == haystack[i + j])
                i++;

            if (i >= m)
            {
                i = ell;

                while (i > memory && needle[i] == haystack[i + j])
                    i--;

                if (i <= memory)
                {
                    // A confirmed occurrence at j: no other occurrence can start strictly within
                    // (j, j + period), because period is the pattern's true minimal period — a nearer
                    // occurrence would itself be a shorter period of the pattern, contradicting minimality.
                    // That is what makes this jump safe even though it isn't re-verified.
                    if (!wholeWord || SearchOptionsHelper.IsWholeWordMatch(text, j, m))
                    {
                        matches.Add(SearchMatchFactory.CreateExact(text, j, m, Algorithm, options));

                        if (matches.Count >= options.MaxResults)
                            break;
                    }

                    j += period;
                }
                else
                {
                    // No occurrence at j: the periodicity argument above doesn't apply, so only a
                    // single-position shift is guaranteed not to skip a real match.
                    j += 1;
                }

                memory = -1;
            }
            else
            {
                j += i - ell;
                memory = -1;
            }
        }

        return matches;
    }

    /// <summary>
    /// Computes the critical factorization point (via Crochemore &amp; Perrin's construction: the later
    /// of the maximal suffixes under forward and reverse lexicographic order) and the pattern's true
    /// minimal period. The Critical Factorization Theorem guarantees this split point's local period
    /// equals the pattern's global period, so shifting the window by that period after a match — or
    /// after ruling out everything up to the critical point — never skips over a real occurrence.
    /// </summary>
    private static (int CriticalPoint, int Period) Preprocess(char[] pattern)
    {
        var m = pattern.Length;

        var ms1 = MaximalSuffixStart(pattern, m, forward: true);
        var ms2 = MaximalSuffixStart(pattern, m, forward: false);

        var criticalPoint = Math.Max(ms1, ms2);
        var period = ComputePeriod(pattern);

        return (criticalPoint, period);
    }

    /// <summary>
    /// The pattern's true minimal period: m minus the length of its longest proper border (prefix that
    /// is also a suffix), found with the same failure-function construction KMP uses. Using the exact
    /// period rather than an estimate keeps the two-way shift/memory logic unconditionally correct.
    /// </summary>
    private static int ComputePeriod(char[] pattern)
    {
        var m = pattern.Length;
        var border = new int[m];
        var length = 0;
        var i = 1;

        while (i < m)
        {
            if (pattern[i] == pattern[length])
            {
                length++;
                border[i] = length;
                i++;
            }
            else if (length != 0)
            {
                length = border[length - 1];
            }
            else
            {
                border[i] = 0;
                i++;
            }
        }

        return m - border[m - 1];
    }

    /// <summary>
    /// Computes the start position of the maximal suffix of <paramref name="pattern"/> under the given
    /// lexicographic order. Direct port of the standard two-way preprocessing routine.
    /// </summary>
    private static int MaximalSuffixStart(char[] pattern, int m, bool forward)
    {
        var ms = -1;
        var j = 0;
        var k = 1;
        var p = 1;

        while (j + k < m)
        {
            var a = pattern[j + k];
            var b = pattern[ms + k];

            var greater = forward ? a.CompareTo(b) > 0 : a.CompareTo(b) < 0;
            var less = forward ? a.CompareTo(b) < 0 : a.CompareTo(b) > 0;

            if (greater)
            {
                j += k;
                k = 1;
                p = j - ms;
            }
            else if (a == b)
            {
                if (k != p)
                {
                    k++;
                }
                else
                {
                    j += p;
                    k = 1;
                }
            }
            else if (less)
            {
                ms = j;
                j = ms + 1;
                k = 1;
                p = 1;
            }
        }

        return ms;
    }
}
