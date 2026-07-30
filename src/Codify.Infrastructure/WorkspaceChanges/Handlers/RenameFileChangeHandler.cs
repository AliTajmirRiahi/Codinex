using System;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;

namespace Codify.Infrastructure.WorkspaceChanges.Handlers;

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

        return WorkspaceChangeResult.Successful();
    }
}