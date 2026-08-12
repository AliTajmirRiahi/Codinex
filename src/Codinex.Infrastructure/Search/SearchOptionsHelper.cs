using System;
using Codinex.Core.Models.Search;

namespace Codinex.Infrastructure.Search;

/// <summary>
/// Shared interpretation of <see cref="StringSearchOptions"/> used by every algorithm implementation,
/// so comparison-mode and whole-word semantics stay identical across strategies.
/// </summary>
internal static class SearchOptionsHelper
{
    public static bool IsCaseInsensitive(StringSearchOptions options)
    {
        return options.ComparisonMode is SearchMode.CaseInsensitive or SearchMode.OrdinalIgnoreCase;
    }

    public static bool IsWholeWord(StringSearchOptions options)
    {
        return options.WholeWord || options.ComparisonMode == SearchMode.WholeWord;
    }

    public static MatchType ResolveMatchType(StringSearchOptions options)
    {
        if (IsWholeWord(options))
            return MatchType.WholeWord;

        return IsCaseInsensitive(options) ? MatchType.CaseInsensitive : MatchType.Exact;
    }

    /// <summary>
    /// Case-folds text into a char array for scanning, using the invariant (not culture-sensitive) casing
    /// rules. Algorithms run their textbook comparison logic over the normalized array while original
    /// <paramref name="text"/>/pattern strings are kept around for substring extraction, so behavior stays
    /// correct even though a small set of exotic casing pairs may not fold identically to true ordinal
    /// case-insensitive comparison.
    /// </summary>
    public static char[] Normalize(string text, bool ignoreCase)
    {
        var chars = text.ToCharArray();

        if (!ignoreCase)
            return chars;

        for (var i = 0; i < chars.Length; i++)
            chars[i] = char.ToUpperInvariant(chars[i]);

        return chars;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>Whether the match at [start, start+length) is bounded by non-word characters or text edges.</summary>
    public static bool IsWholeWordMatch(string text, int start, int length)
    {
        if (start > 0 && IsWordChar(text[start - 1]))
            return false;

        var end = start + length;

        if (end < text.Length && IsWordChar(text[end]))
            return false;

        return true;
    }

    public static void ValidateTextAndPattern(string text, string pattern)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        if (pattern.Length == 0)
            throw new ArgumentException("Pattern must not be empty.", nameof(pattern));
    }

    public static void ValidateOptions(StringSearchOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
    }
}
