using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.Helper;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Infrastructure.WorkspaceChanges.Resolution;


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

            var content = await workspaceFileService.ReadAsync(
                editFileChange.FilePath,
                cancellationToken);

            content = stringHelper.Normalize(content);

            var resolvedTextChanges = new List<ResolvedTextChange>();

            foreach (var textChange in editFileChange.TextChanges.OrderBy(x => x.Order))
            {
                var match = textChangeMatcher.Match(
                    content,
                    textChange);

                if (match.Status != TextChangeMatchStatus.Success)
                    return ChangeValidationResult.Failed(match.Error);

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
            }

            resolvedFileChanges.Add(new ResolvedFileChange()
            {
                FilePath = editFileChange.FilePath,
                TextChanges = resolvedTextChanges
            });
        }

        return ChangeValidationResult.Successful(resolvedFileChanges);
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
