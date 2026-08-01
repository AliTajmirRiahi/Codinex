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

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class RenameFileChangeHandler(
    IWorkspaceFileService workspaceFileService)
    : IWorkspaceChangeHandler<RenameFileChange>
{
    public async Task<WorkspaceChangeResult> HandleAsync(
        RenameFileChange change,
        CancellationToken cancellationToken = default)
    {
        if (change == null)
            throw new ArgumentNullException(nameof(change));

        await workspaceFileService.RenameAsync(
            change.FilePath,
            change.NewFileName,
            cancellationToken);

        return WorkspaceChangeResult.Successful(new WorkspaceChangeSuccess()
        {
            Files = [new ChangedFileResult()
            {
                Operation = "RenameFile",
                Path = change.FilePath,
            }]
        });
    }
}