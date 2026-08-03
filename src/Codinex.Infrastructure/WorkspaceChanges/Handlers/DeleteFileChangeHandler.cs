using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Handlers;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class DeleteFileChangeHandler(
    IWorkspaceFileService workspaceFileService,
    IWorkspaceChangeErrorFactory errorFactory)
    : IWorkspaceChangeHandler<DeleteFileChange>
{
    public async Task<WorkspaceChangeResult> HandleAsync(
        DeleteFileChange change,
        CancellationToken cancellationToken = default)
    {
        if (!workspaceFileService.Exists(change.FilePath))
        {
            return WorkspaceChangeResult.Failed(
                errorFactory.Create(
                    WorkspaceChangeErrorCode.FileNotFound,
                    change.FilePath,
                    change.Id,
                    $"The file '{change.FilePath}' does not exist."));
        }

        await workspaceFileService.DeleteAsync(
            change.FilePath,
            cancellationToken);

        return WorkspaceChangeResult.Successful(new WorkspaceChangeSuccess()
        {
            Files = [new ChangedFileResult()
            {
                Operation = "DeleteFile",
                Path = change.FilePath,
            }]
        });
    }
}