using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.Helper;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Resolution;

/// <summary>
/// Find + Validate + Plan for every EditFileChange's TextFileChange entries, run before
/// Review is shown.
///
/// Find: Search is matched against the current file content; when it fails (not found, or
/// found more than once), Target is tried as a fallback anchor.
///
/// Validate: whichever locator is used must match exactly one location — otherwise
/// resolution fails for the whole changeset and Review must never open.
///
/// Plan: for the winning location, computes the exact <see cref="TextRange"/> together with
/// the text currently occupying it and the text that will occupy it afterwards. Every
/// operation reduces to the same shape — "replace this range with this text" — so an
/// applier never has to branch on Operation.
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
    ITextChangeMatcher textChangeMatcher,
    IStringHelper stringHelper)
    : IEditFileChangeResolver
{
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
            var locator = Locate(content, textChange);

            if (locator == null)
            {
                return (null, new ChangeValidationError(
                    textChange.Id,
                    WorkspaceChangeErrorCode.SearchNotFound,
                    WorkspaceValidationCategory.AiRecoverable,
                    $"Could not uniquely locate text change #{textChange.Order} in " +
                    $"'{change.FilePath}' using either Search or Target. Search must match " +
                    "exactly one location in the current file; retry with a corrected, " +
                    "unique Search (and optionally Target)."));
            }

            var (match, winningText) = locator.Value;

            var operation = ParseOperation(textChange.Operation);

            var (range, originalText, resultText) = Plan(content, match, operation, textChange.Content);

            resolvedTextChanges.Add(new ResolvedTextChange
            {
                Id = textChange.Id,
                Order = textChange.Order,
                Operation = operation,
                Target = textChange.Target,
                Search = winningText,
                Content = textChange.Content,
                Range = range,
                OriginalText = originalText,
                ResultText = resultText
            });

            // Persist the winning, already-unique locator so preview and apply resolve to
            // this exact same location when they re-match on Search.
            textChange.Search = winningText;

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
    /// Search-first, Target-fallback resolution. Returns null when neither locator
    /// uniquely identifies a single location.
    /// </summary>
    private (TextChangeMatchResult Match, string Text)? Locate(
        string content,
        TextFileChange textChange)
    {
        if (!string.IsNullOrWhiteSpace(textChange.Search))
        {
            var searchMatch = textChangeMatcher.MatchText(content, textChange.Search);

            if (searchMatch.Status == TextChangeMatchStatus.Success)
                return (searchMatch, textChange.Search);
        }

        //if (!string.IsNullOrWhiteSpace(textChange.Target))
        //{
        //    var targetMatch = textChangeMatcher.MatchText(content, textChange.Target);

        //    if (targetMatch.Status == TextChangeMatchStatus.Success)
        //        return (targetMatch, textChange.Target);
        //}

        return null;
    }

    private static ChangeOperation ParseOperation(string operation)
    {
        return operation switch
        {
            TextChangeOperations.InsertBefore => ChangeOperation.InsertBefore,
            TextChangeOperations.InsertAfter => ChangeOperation.InsertAfter,
            TextChangeOperations.Delete => ChangeOperation.Delete,
            _ => ChangeOperation.Replace
        };
    }

    /// <summary>
    /// Reduces any operation to a single uniform shape: the range in <paramref name="content"/>
    /// that gets replaced, and the text it gets replaced with.
    /// </summary>
    private static (TextRange Range, string OriginalText, string ResultText) Plan(
        string content,
        TextChangeMatchResult match,
        ChangeOperation operation,
        string changeContent)
    {
        int start;
        int length;
        string resultText;

        switch (operation)
        {
            case ChangeOperation.InsertBefore:
                start = match.StartIndex;
                length = 0;
                resultText = changeContent ?? string.Empty;
                break;

            case ChangeOperation.InsertAfter:
                start = match.StartIndex + match.Length;
                length = 0;
                resultText = changeContent ?? string.Empty;
                break;

            case ChangeOperation.Delete:
                start = match.StartIndex;
                length = match.Length;
                resultText = string.Empty;
                break;

            default: // Replace
                start = match.StartIndex;
                length = match.Length;
                resultText = changeContent ?? string.Empty;
                break;
        }

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
