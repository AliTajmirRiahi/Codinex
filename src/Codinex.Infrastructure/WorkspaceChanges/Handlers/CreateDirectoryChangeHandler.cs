using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Handlers;

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