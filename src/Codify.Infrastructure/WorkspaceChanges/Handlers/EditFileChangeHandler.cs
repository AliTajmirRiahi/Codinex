using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Handlers;

/// <summary>
/// Handles file text modifications.
/// </summary>
[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class EditFileChangeHandler(
    IWorkspaceFileService workspaceFileService,
    ITextChangeMatcher textChangeMatcher,
    IWorkspaceChangeErrorFactory workspaceChangeErrorFactory)
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

        foreach (var textChange in change.TextChanges.OrderBy(x => x.Order))
        {
            var match = textChangeMatcher.Match(
                content,
                textChange);

            if (match.Status != TextChangeMatchStatus.Success)
            {
                return WorkspaceChangeResult.Failed(
                    workspaceChangeErrorFactory.Create(
                        match.Status.ToWorkspaceChangeErrorCode(),
                        change.FilePath,
                        textChange.Id,
                        match.Error));
            }

            content = ApplyReplacement(
                content,
                match,
                textChange);
        }

        await workspaceFileService.WriteAsync(
            change.FilePath,
            content,
            cancellationToken: cancellationToken);

        return WorkspaceChangeResult.Successful();
    }

    private static string ApplyReplacement(
        string content,
        TextChangeMatchResult match,
        TextFileChange change)
    {
        return content
            .Remove(match.StartIndex, match.Length)
            .Insert(match.StartIndex, change.Replace);
    }
}