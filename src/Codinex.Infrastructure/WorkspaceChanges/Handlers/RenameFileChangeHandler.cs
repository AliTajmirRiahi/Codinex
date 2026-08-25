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