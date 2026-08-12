using System;
using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Approximate (edit-distance) search. Kept entirely separate from the exact-match algorithms — no
/// exact-search strategy mixes in fuzzy logic, and this strategy never claims to be an exact match.
/// </summary>
/// <remarks>
/// Two phases:
/// 1. Sellers' online approximate-matching DP finds every text position whose distance-to-pattern
///    (over some suffix ending there) is within <see cref="StringSearchOptions.MaxEditDistance"/>,
///    reporting only local minima so one approximate occurrence doesn't produce a run of near-duplicate
///    candidates.
/// 2. For each candidate end position, a small window of candidate lengths around the pattern length is
///    each scored with full Levenshtein distance to recover the best-matching start offset.
/// <para>Complexity: O(n*m) for phase 1, O(k*m) additional work per candidate in phase 2 where k is the
/// small window of candidate lengths (bounded by 2*MaxEditDistance+1).</para>
/// </remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class FuzzyStringSearchAlgorithm : IFuzzyStringSearchAlgorithm
{
    public SearchAlgorithmType Algorithm => SearchAlgorithmType.Fuzzy;

    public string Name => "Fuzzy (Levenshtein)";

    public IReadOnlyList<SearchMatch> Search(string text, string pattern, StringSearchOptions options)
    {
        SearchOptionsHelper.ValidateTextAndPattern(text, pattern);
        SearchOptionsHelper.ValidateOptions(options);

        var matches = new List<SearchMatch>();

        var ignoreCase = SearchOptionsHelper.IsCaseInsensitive(options);
        var haystack = SearchOptionsHelper.Normalize(text, ignoreCase);
        var needle = SearchOptionsHelper.Normalize(pattern, ignoreCase);
        var wholeWord = SearchOptionsHelper.IsWholeWord(options);

        var m = needle.Length;
        var n = haystack.Length;
        var maxDistance = Math.Max(0, options.MaxEditDistance);

        if (n == 0)
            return matches;

        var endPositions = FindCandidateEndPositions(haystack, needle, maxDistance);

        foreach (var (endIndexInclusive, _) in endPositions)
        {
            var candidate = FindBestWindow(text, haystack, needle, endIndexInclusive, maxDistance);

            if (candidate == null)
                continue;

            var (start, length, distance) = candidate.Value;

            var score = 1.0 - distance / (double)Math.Max(m, length);

            if (score < options.MinimumScore)
                continue;

            if (wholeWord && !SearchOptionsHelper.IsWholeWordMatch(text, start, length))
                continue;

            matches.Add(SearchMatchFactory.CreateFuzzy(text, start, length, score, Algorithm));

            if (matches.Count >= options.MaxResults)
                break;
        }

        return matches;
    }

    /// <summary>
    /// Sellers' algorithm: dp[j] holds the edit distance between pattern[0..j) and the best-matching
    /// suffix of the text scanned so far. dp[0] is pinned to 0 every step, which is what turns plain
    /// Levenshtein distance into a *search* (a match can start anywhere, not just at text position 0).
    /// </summary>
    private static List<(int EndIndex, int Distance)> FindCandidateEndPositions(char[] haystack, char[] needle, int maxDistance)
    {
        var m = needle.Length;
        var dp = new int[m + 1];
        var previousDp = new int[m + 1];

        for (var j = 0; j <= m; j++)
            dp[j] = j;

        var results = new List<(int, int)>();
        var previousQualified = false;
        var previousDistance = int.MaxValue;

        for (var i = 0; i < haystack.Length; i++)
        {
            (previousDp, dp) = (dp, previousDp);
            dp[0] = 0;

            for (var j = 1; j <= m; j++)
            {
                var cost = haystack[i] == needle[j - 1] ? 0 : 1;

                var substitution = previousDp[j - 1] + cost;
                var deletion = previousDp[j] + 1;
                var insertion = dp[j - 1] + 1;

                dp[j] = Math.Min(substitution, Math.Min(deletion, insertion));
            }

            var qualifies = dp[m] <= maxDistance;

            // Only emit a local minimum: if the next position could still improve on this one, wait for it.
            if (previousQualified && (!qualifies || dp[m] > previousDistance))
                results.Add((i - 1, previousDistance));

            previousQualified = qualifies;
            previousDistance = dp[m];
        }

        if (previousQualified)
            results.Add((haystack.Length - 1, previousDistance));

        return results;
    }

    /// <summary>Recovers the best start offset/length for an approximate match ending at <paramref name="endIndexInclusive"/>.</summary>
    private static (int Start, int Length, int Distance)? FindBestWindow(
        string originalText,
        char[] haystack,
        char[] needle,
        int endIndexInclusive,
        int maxDistance)
    {
        var m = needle.Length;
        var minLength = Math.Max(1, m - maxDistance);
        var maxLength = m + maxDistance;

        (int Start, int Length, int Distance)? best = null;

        for (var length = minLength; length <= maxLength; length++)
        {
            var start = endIndexInclusive - length + 1;

            if (start < 0)
                continue;

            var distance = LevenshteinDistance(haystack, start, length, needle);

            if (distance > maxDistance)
                continue;

            if (best == null || distance < best.Value.Distance)
                best = (start, length, distance);
        }

        return best;
    }

    private static int LevenshteinDistance(char[] haystack, int start, int length, char[] needle)
    {
        var m = needle.Length;
        var previous = new int[m + 1];
        var current = new int[m + 1];

        for (var j = 0; j <= m; j++)
            previous[j] = j;

        for (var i = 1; i <= length; i++)
        {
            current[0] = i;
            var haystackChar = haystack[start + i - 1];

            for (var j = 1; j <= m; j++)
            {
                var cost = haystackChar == needle[j - 1] ? 0 : 1;

                current[j] = Math.Min(
                    previous[j] + 1,
                    Math.Min(current[j - 1] + 1, previous[j - 1] + cost));
            }

            (previous, current) = (current, previous);
        }

        return previous[m];
    }
}
