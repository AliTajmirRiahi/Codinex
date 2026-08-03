using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.Tools;
using Codify.Core.Models.WorkspaceChanges;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges.Handlers;

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