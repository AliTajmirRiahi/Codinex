using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.Tools;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Handlers;

/// <summary>
/// Handles directory creation operations.
/// </summary>
[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class CreateDirectoryChangeHandler(
    IWorkspaceFileService workspaceFileService,
    IWorkspaceChangeErrorFactory workspaceChangeErrorFactory)
    : IWorkspaceChangeHandler<CreateDirectoryChange>
{
    public async Task<WorkspaceChangeResult> HandleAsync(
        CreateDirectoryChange change,
        CancellationToken cancellationToken = default)
    {
        if (change == null)
        {
            throw new ArgumentNullException(nameof(change));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (workspaceFileService.DirectoryExists(change.DirectoryPath))
        {
            return await Task.FromResult(WorkspaceChangeResult.Failed(
                workspaceChangeErrorFactory.Create(
                    WorkspaceChangeErrorCode.DirectoryAlreadyExists,
                    change.DirectoryPath,
                    change.Id,
                    $"The directory '{change.DirectoryPath}' already exists.")));
        }

        await workspaceFileService.CreateDirectoryAsync(
            change.DirectoryPath,
            cancellationToken);

        return WorkspaceChangeResult.Successful(new WorkspaceChangeSuccess()
        {
            Files = [new ChangedFileResult()
            {
                Operation = "CreateDirectory",
                Path = change.DirectoryPath,
            }]
        });
    }
}