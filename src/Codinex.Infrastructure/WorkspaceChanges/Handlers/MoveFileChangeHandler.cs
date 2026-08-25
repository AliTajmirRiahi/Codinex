using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Handlers;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class MoveFileChangeHandler(
    IWorkspaceFileService workspaceFileService,
    IWorkspaceChangeErrorFactory workspaceChangeErrorFactory)
    : IWorkspaceChangeHandler<MoveFileChange>
{
    public async Task<WorkspaceChangeResult> HandleAsync(
        MoveFileChange change,
        CancellationToken cancellationToken = default)
    {
        if (change == null)
        {
            throw new ArgumentNullException(nameof(change));
        }

        if (!workspaceFileService.Exists(change.SourcePath))
        {
            return WorkspaceChangeResult.Failed(
                workspaceChangeErrorFactory.Create(
                    WorkspaceChangeErrorCode.FileNotFound,
                    change.SourcePath,
                    change.Id,
                    $"The file '{change.SourcePath}' does not exist."));
        }

        if (workspaceFileService.Exists(change.DestinationPath))
        {
            return WorkspaceChangeResult.Failed(
                workspaceChangeErrorFactory.Create(
                    WorkspaceChangeErrorCode.FileAlreadyExists,
                    change.DestinationPath,
                    change.Id,
                    $"The file '{change.DestinationPath}' already exists."));
        }

        await workspaceFileService.MoveAsync(
            change.SourcePath,
            change.DestinationPath,
            cancellationToken);

        return WorkspaceChangeResult.Successful(new WorkspaceChangeSuccess()
        {
            Files = [new ChangedFileResult()
            {
                Operation = "MoveFile",
                Path = change.DestinationPath,
            }]
        });
    }
}