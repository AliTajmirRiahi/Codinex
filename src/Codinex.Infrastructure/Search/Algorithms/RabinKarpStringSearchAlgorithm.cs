using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search.Algorithms;

/// <summary>
/// Rabin-Karp: rolling-hash search. A hash match is only a candidate — the substring is always
/// verified character-by-character before being reported, so hash collisions can never produce a
/// false positive.
/// </summary>
/// <remarks>Complexity: preprocessing O(m), search O(n + m) average case, O(n*m) worst case under
/// pathological collisions (bounded in practice by the verification step, which caps wasted work per
/// collision at O(m)).</remarks>
[AutoDiRegister(Modules.Search, RegistrationOrder.Infrastructure)]
public sealed class RabinKarpStringSearchAlgorithm : IStringSearchAlgorithm
{
    // Large prime modulus and a base coprime to it keep the rolling hash well-distributed over typical
    // source-code alphabets while avoiding overflow (both fit comfortably in long arithmetic).
    private const long Modulus = 1_000_000_007L;
    private const long Base = 256L;

    public SearchAlgorithmType Algorithm => SearchAlgorithmType.RabinKarp;

    public string Name => "Rabin-Karp";

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

        var highOrder = 1L;

        for (var i = 0; i < m - 1; i++)
            highOrder = highOrder * Base % Modulus;

        var patternHash = 0L;
        var windowHash = 0L;

        for (var i = 0; i < m; i++)
        {
            patternHash = (patternHash * Base + needle[i]) % Modulus;
            windowHash = (windowHash * Base + haystack[i]) % Modulus;
        }

        for (var s = 0; s <= n - m; s++)
        {
            if (windowHash == patternHash && MatchesAt(haystack, needle, s))
            {
                if (!wholeWord || SearchOptionsHelper.IsWholeWordMatch(text, s, m))
                {
                    matches.Add(SearchMatchFactory.CreateExact(text, s, m, Algorithm, options));

                    if (matches.Count >= options.MaxResults)
                        break;
                }
            }

            if (s < n - m)
            {
                windowHash = (windowHash - haystack[s] * highOrder % Modulus + Modulus) % Modulus;
                windowHash = (windowHash * Base + haystack[s + m]) % Modulus;
            }
        }

        return matches;
    }

    /// <summary>Verifies an actual substring match; a hash collision alone is never treated as a match.</summary>
    private static bool MatchesAt(char[] haystack, char[] needle, int start)
    {
        for (var i = 0; i < needle.Length; i++)
        {
            if (haystack[start + i] != needle[i])
                return false;
        }

        return true;
    }
}
