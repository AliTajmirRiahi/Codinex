using System;
using System.Collections.Generic;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class TextChangeMatcher : ITextChangeMatcher
{
    public TextChangeMatchResult Match(
        string content,
        TextFileChange change)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (change == null)
            throw new ArgumentNullException(nameof(change));

        if (string.IsNullOrWhiteSpace(change.Search))
            return new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.NoUniqueMatch,
                MatchCount = 0,
                Error = "Unable to search with empty text."
            };

        var matches = FindMatches(content, change.Search);

        if (matches.Count == 0)
        {
            return new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.SearchNotFound,
                MatchCount = 0,
                Error = "Search text was not found."
            };
        }

        matches = FilterByBefore(content, matches, change.Before);
        matches = FilterByAfter(content, matches, change.After);

        return matches.Count switch
        {
            0 => new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.NoUniqueMatch,
                MatchCount = 0,
                Error = "Unable to uniquely identify the requested text."
            },

            1 => BuildSuccessResult(content, matches[0]),

            _ => new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.MultipleMatches,
                MatchCount = matches.Count,
                Error = "Multiple matching locations were found."
            }
        };
    }

    /// <summary>
    /// Matches arbitrary locator text (e.g. a TextFileChange's Search value) against file content,
    /// independent of any specific TextFileChange — so no Before/After disambiguation is applied.
    /// </summary>
    public TextChangeMatchResult MatchText(
        string content,
        string text)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(text))
            return new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.NoUniqueMatch,
                MatchCount = 0,
                Error = "Unable to search with empty text."
            };

        var matches = FindMatches(content, text);

        if (matches.Count == 0)
        {
            return new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.SearchNotFound,
                MatchCount = 0,
                Error = "Search text was not found."
            };
        }

        return matches.Count switch
        {
            0 => new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.NoUniqueMatch,
                MatchCount = 0,
                Error = "Unable to uniquely identify the requested text."
            },

            1 => BuildSuccessResult(content, matches[0]),

            _ => new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.MultipleMatches,
                MatchCount = matches.Count,
                Error = "Multiple matching locations were found."
            }
        };
    }

    private static List<TextMatch> FindMatches(
        string content,
        string search)
    {
        var matches = new List<TextMatch>();

        var index = 0;

        while (true)
        {
            index = content.IndexOf(search, index, StringComparison.Ordinal);

            if (index < 0)
                break;

            matches.Add(new TextMatch
            {
                Start = index,
                Length = search.Length
            });

            // Find non-overlapping matches only.
            index += search.Length;
        }

        return matches;
    }

    private static List<TextMatch> FilterByBefore(
        string content,
        IReadOnlyList<TextMatch> matches,
        string before)
    {
        if (string.IsNullOrEmpty(before))
            return matches.ToList();

        return matches
            .Where(match =>
            {
                if (match.Start < before.Length)
                    return false;

                return string.Equals(
                    content.Substring(match.Start - before.Length, before.Length),
                    before,
                    StringComparison.Ordinal);
            })
            .ToList();
    }

    private static List<TextMatch> FilterByAfter(
        string content,
        IReadOnlyList<TextMatch> matches,
        string after)
    {
        if (string.IsNullOrEmpty(after))
            return matches.ToList();

        return matches
            .Where(match =>
            {
                var start = match.Start + match.Length;

                if (start + after.Length > content.Length)
                    return false;

                return string.Equals(
                    content.Substring(start, after.Length),
                    after,
                    StringComparison.Ordinal);
            })
            .ToList();
    }

    private static TextChangeMatchResult BuildSuccessResult(
        string content,
        TextMatch match)
    {
        return new TextChangeMatchResult
        {
            Status = TextChangeMatchStatus.Success,
            MatchCount = 1,
            StartIndex = match.Start,
            Length = match.Length,
            MatchedText = content.Substring(match.Start, match.Length),
            Error = null
        };
    }

    private sealed class TextMatch
    {
        public int Start { get; set; }

        public int Length { get; set; }
    }
}
