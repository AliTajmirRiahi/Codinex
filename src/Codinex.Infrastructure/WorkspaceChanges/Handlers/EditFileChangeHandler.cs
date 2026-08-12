using Codinex.Infrastructure.Extensions;
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
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Handlers;

/// <summary>
/// Handles file text modifications.
/// </summary>
[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class EditFileChangeHandler(
    IWorkspaceFileService workspaceFileService,
    ITextChangeMatcher textChangeMatcher,
    IWorkspaceChangeErrorFactory workspaceChangeErrorFactory,
    IStringHelper stringHelper,
    ITextChangeApplier textChangeApplier,
    IEditFileChangeResolutionContext resolutionContext)
    : IWorkspaceChangeHandler<EditFileChange>
{

    public async Task<WorkspaceChangeResult> HandleAsync(
        EditFileChange change,
        CancellationToken cancellationToken)
    {
        if (change == null)
        {
            throw new ArgumentNullException(nameof(change));
        }

        var content = await workspaceFileService.ReadAsync(
            change.FilePath,
            cancellationToken);

        content = stringHelper.Normalize(content);

        List<ChangedFileResult> changedFileResults = [];

        foreach (var textChange in change.TextChanges.OrderBy(x => x.Order))
        {
            var match = textChangeMatcher.Match(
                content,
                textChange);

            if (match.Status == TextChangeMatchStatus.Success)
            {
                content = textChangeApplier.Apply(
                    content,
                    match,
                    textChange);

                continue;
            }

            // Search no longer uniquely finds this change (e.g. the file changed since the
            // changeset was resolved) — fall back to the already-validated plan instead of
            // failing outright.
            var resolvedChange = FindResolved(change.FilePath, textChange.Id);

            if (resolvedChange == null)
            {
                return WorkspaceChangeResult.Failed(
                    workspaceChangeErrorFactory.Create(
                        match.Status.ToWorkspaceChangeErrorCode(),
                        change.FilePath,
                        textChange.Id,
                        match.Error));
            }

            try
            {
                content = content
                    .Remove(resolvedChange.Range.Start, resolvedChange.Range.Length)
                    .Insert(resolvedChange.Range.Start, resolvedChange.ResultText);
            }
            catch (ArgumentOutOfRangeException)
            {
                return WorkspaceChangeResult.Failed(
                    workspaceChangeErrorFactory.Create(
                        match.Status.ToWorkspaceChangeErrorCode(),
                        change.FilePath,
                        textChange.Id,
                        $"{match.Error} The resolved fallback location no longer fits the " +
                        "current file content either."));
            }
        }

        await workspaceFileService.WriteAsync(
            change.FilePath,
            content,
            cancellationToken: cancellationToken);

        changedFileResults.Add(new ChangedFileResult()
        {
            Operation = "EditFile",
            Path = change.FilePath,
        });

        return WorkspaceChangeResult.Successful(new WorkspaceChangeSuccess()
        {
            Files = changedFileResults
        });
    }

    private ResolvedTextChange FindResolved(string filePath, Guid textChangeId)
    {
        var resolvedFile = resolutionContext.Current?.Changes.FirstOrDefault(x =>
            string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        return resolvedFile?.TextChanges.FirstOrDefault(x => x.Id == textChangeId);
    }
}