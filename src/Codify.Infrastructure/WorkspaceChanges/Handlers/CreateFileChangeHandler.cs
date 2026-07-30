using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Handlers;

/// <summary>
/// Handles file creation operations.
/// </summary>
[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class CreateFileChangeHandler(
    IWorkspaceFileService workspaceFileService,
    IWorkspaceChangeErrorFactory workspaceChangeErrorFactory)
    : IWorkspaceChangeHandler<CreateFileChange>
{
    public async Task<WorkspaceChangeResult> HandleAsync(
        CreateFileChange change,
        CancellationToken cancellationToken)
    {
        if (change == null)
        {
            throw new ArgumentNullException(nameof(change));
        }

        if (workspaceFileService.Exists(change.FilePath))
        {
            return WorkspaceChangeResult.Failed(
                workspaceChangeErrorFactory.Create(
                    WorkspaceChangeErrorCode.FileAlreadyExists,
                    change.FilePath,
                    change.Id,
                    $"The file '{change.FilePath}' already exists."));
        }

        await workspaceFileService.WriteAsync(
            change.FilePath,
            change.Content ?? string.Empty,
            null,
            cancellationToken);

        return WorkspaceChangeResult.Successful();
    }
}