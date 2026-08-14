using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.Helper;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.Search;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Resolution;

/// <summary>
/// Find + Validate + Plan for every EditFileChange's TextFileChange entries, run before
/// Review is shown.
///
/// Find: Search is matched against the current file content via every exact algorithm the
/// search engine has; only locations every algorithm agrees on are trusted. Before/After
/// disambiguate when Search alone matches more than one location.
///
/// Validate: the agreed-upon locator must match at least one location — otherwise resolution
/// fails for the whole changeset and Review must never open. When more than one candidate
/// survives Before/After disambiguation, the earliest occurrence in the file is used rather
/// than failing resolution.
///
/// Plan: for the winning location, computes the exact <see cref="TextRange"/> together with
/// the text currently occupying it and the text that will occupy it afterwards (Replace).
///
/// The winning locator text is also written back onto TextFileChange.Search. Preview and
/// apply both still re-match on Search against a freshly-read file rather than trusting the
/// numeric Range from resolution time verbatim — that keeps them self-healing if the file on
/// disk changes during the (potentially long) human review pause between resolution and
/// apply, while still resolving to the exact same, already-validated location when it hasn't.
/// </summary>
[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class EditFileChangeResolver(
    IWorkspaceFileService workspaceFileService,
    ICodeSearchEngine codeSearchEngine,
    IStringHelper stringHelper)
    : IEditFileChangeResolver
{
    /// <summary>
    /// Every exact-match strategy the search engine has, tried independently and reconciled to a
    /// single agreed-upon set of locations. Resolution decides whether a changeset is safe to apply,
    /// so a wrong location here is worse than the extra work of cross-checking it — a single
    /// algorithm's bug (or an edge case one implementation handles differently) can't silently produce
    /// a bad match, because it would first have to survive agreement with every other algorithm.
    /// Bitap/BNDM are bit-parallel and reject patterns over 64 characters; they're skipped for longer
    /// Search text rather than treated as a failure.
    /// </summary>
    private static readonly SearchAlgorithmType[] ExactAlgorithms =
    [
        SearchAlgorithmType.Naive,
        SearchAlgorithmType.Kmp,
        SearchAlgorithmType.BoyerMoore,
        SearchAlgorithmType.BoyerMooreHorspool,
        SearchAlgorithmType.RabinKarp,
        SearchAlgorithmType.TwoWay,
        SearchAlgorithmType.Bitap,
        SearchAlgorithmType.Bndm
    ];

    public async Task<ChangeValidationResult> ResolveAsync(
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken = default)
    {
        if (changeSet == null)
            throw new ArgumentNullException(nameof(changeSet));

        var resolvedFileChanges = new List<ResolvedFileChange>();

        foreach (var change in changeSet.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (change is not EditFileChange editFileChange)
                continue;

            var (resolved, error) = await ResolveEditFileChangeAsync(editFileChange, cancellationToken);

            if (error != null)
                return ChangeValidationResult.Failed(error);

            resolvedFileChanges.Add(resolved);
        }

        return ChangeValidationResult.Successful(resolvedFileChanges);
    }

    private async Task<(ResolvedFileChange Resolved, ChangeValidationError Error)> ResolveEditFileChangeAsync(
        EditFileChange change,
        CancellationToken cancellationToken)
    {
        string content;

        try
        {
            content = stringHelper.Normalize(
                await workspaceFileService.ReadAsync(change.FilePath, cancellationToken));
        }
        catch (Exception ex)
        {
            return (null, new ChangeValidationError(
                change.Id,
                WorkspaceChangeErrorCode.FileNotFound,
                WorkspaceValidationCategory.AiRecoverable,
                $"Could not read '{change.FilePath}': {ex.Message}"));
        }

        var resolvedTextChanges = new List<ResolvedTextChange>();

        foreach (var textChange in change.TextChanges.OrderBy(x => x.Order))
        {
            var match = MatchText(content, textChange.Search, textChange.Before, textChange.After);

            if (match.Status != TextChangeMatchStatus.Success)
            {
                return (null, new ChangeValidationError(
                    textChange.Id,
                    WorkspaceChangeErrorCode.SearchNotFound,
                    WorkspaceValidationCategory.AiRecoverable,
                    $"Could not locate text change #{textChange.Order} in '{change.FilePath}'. " +
                    "Search did not match anywhere in the current file (optionally narrowed with " +
                    "Before/After); retry with a corrected Search."));
            }

            var (range, originalText, resultText) = Plan(content, match, textChange.Replace);

            resolvedTextChanges.Add(new ResolvedTextChange
            {
                Id = textChange.Id,
                Order = textChange.Order,
                Search = textChange.Search,
                Replace = textChange.Replace,
                Range = range,
                OriginalText = originalText,
                ResultText = resultText
            });

            // Advance the working copy so later text changes in this file are validated
            // and planned against the post-edit content, exactly mirroring how apply runs.
            content = content
                .Remove(range.Start, range.Length)
                .Insert(range.Start, resultText);
        }

        return (new ResolvedFileChange
        {
            FilePath = change.FilePath,
            TextChanges = resolvedTextChanges
        }, null);
    }

    /// <summary>
    /// Runs <paramref name="search"/> through every registered exact algorithm and keeps only the
    /// locations every one of them agrees on, then narrows further with Before/After when either is
    /// given — mirroring <c>TextChangeMatcher</c>'s disambiguation, just backed by the full algorithm
    /// roster instead of a single IndexOf scan. No agreed location is SearchNotFound; one or more is
    /// Success — when several candidates remain, the earliest occurrence wins rather than failing
    /// resolution (see the selection below). <see cref="TextChangeMatchResult.MatchCount"/> still
    /// reports how many candidates existed, in case a caller wants to know it was ambiguous.
    /// </summary>
    private TextChangeMatchResult MatchText(string content, string search, string before, string after)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(search))
        {
            return new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.NoUniqueMatch,
                MatchCount = 0,
                Error = "Unable to search with empty text."
            };
        }

        HashSet<int> agreedStarts = null;

        foreach (var algorithm in ExactAlgorithms)
        {
            IReadOnlyList<SearchMatch> matches;

            try
            {
                matches = codeSearchEngine.Search(new SearchRequest
                {
                    Text = content,
                    Pattern = search,
                    Options = new StringSearchOptions
                    {
                        Algorithm = algorithm,
                        ComparisonMode = SearchMode.Exact
                    }
                }).Matches;
            }
            catch (NotSupportedException)
            {
                // Bitap/BNDM cap pattern length at one machine word; skip them for longer Search text
                // rather than failing resolution over an algorithm-specific limitation.
                continue;
            }

            var starts = matches.Select(m => m.StartIndex).ToHashSet();

            agreedStarts = agreedStarts == null
                ? starts
                : [..agreedStarts.Intersect(starts)];
        }

        agreedStarts = FilterByBefore(content, agreedStarts ?? [], search.Length, before);
        agreedStarts = FilterByAfter(content, agreedStarts, search.Length, after);

        var matchCount = agreedStarts.Count;

        if (matchCount == 0)
        {
            return new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.SearchNotFound,
                MatchCount = 0,
                Error = "Search text was not found."
            };
        }

        // Multiple locations still agree after Before/After disambiguation — rather than failing
        // resolution outright, take the best candidate: the earliest occurrence in the file. Editors
        // and "find next" tooling default to top-down order, so it's the least surprising choice when
        // nothing else distinguishes the candidates.
        var start = agreedStarts.Min();

        return new TextChangeMatchResult
        {
            Status = TextChangeMatchStatus.Success,
            MatchCount = matchCount,
            StartIndex = start,
            Length = search.Length,
            MatchedText = content.Substring(start, search.Length),
            Error = null
        };
    }

    private static HashSet<int> FilterByBefore(string content, HashSet<int> starts, int searchLength, string before)
    {
        if (string.IsNullOrEmpty(before))
            return starts;

        return starts
            .Where(start =>
                start >= before.Length &&
                string.Equals(
                    content.Substring(start - before.Length, before.Length),
                    before,
                    StringComparison.Ordinal))
            .ToHashSet();
    }

    private static HashSet<int> FilterByAfter(string content, HashSet<int> starts, int searchLength, string after)
    {
        if (string.IsNullOrEmpty(after))
            return starts;

        return starts
            .Where(start =>
            {
                var end = start + searchLength;

                return end + after.Length <= content.Length &&
                    string.Equals(
                        content.Substring(end, after.Length),
                        after,
                        StringComparison.Ordinal);
            })
            .ToHashSet();
    }

    /// <summary>
    /// Replaces the matched Search span with Replace, computing the exact range and the text on
    /// either side of the edit.
    /// </summary>
    private static (TextRange Range, string OriginalText, string ResultText) Plan(
        string content,
        TextChangeMatchResult match,
        string replace)
    {
        var start = match.StartIndex;
        var length = match.Length;
        var resultText = replace ?? string.Empty;

        var originalText = content.Substring(start, length);

        var (startLine, startColumn) = GetLineColumn(content, start);
        var (endLine, endColumn) = GetLineColumn(content, start + length);

        var range = new TextRange
        {
            Start = start,
            Length = length,
            StartLine = startLine,
            StartColumn = startColumn,
            EndLine = endLine,
            EndColumn = endColumn
        };

        return (range, originalText, resultText);
    }

    /// <summary>1-based line/column for an offset into <paramref name="content"/>.</summary>
    private static (int Line, int Column) GetLineColumn(string content, int offset)
    {
        var line = 1;
        var lastNewLineIndex = -1;

        for (var i = 0; i < offset; i++)
        {
            if (content[i] != '\n')
                continue;

            line++;
            lastNewLineIndex = i;
        }

        var column = offset - lastNewLineIndex;

        return (line, column);
    }
}
