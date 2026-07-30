using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges;

public sealed class WorkspaceChangeApplier(
    IWorkspaceChangeHandlerInvoker workspaceChangeHandlerInvoker)
    : IWorkspaceChangeApplier
{
    public async Task<WorkspaceChangeResult> ApplyAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default)
    {
        if (workspaceChangeSet == null)
            throw new ArgumentNullException(nameof(workspaceChangeSet));

        foreach (var workspaceChange in workspaceChangeSet.Changes)
        {
            var result =
                await workspaceChangeHandlerInvoker.InvokeAsync(
                    workspaceChange,
                    cancellationToken);

            if (!result.Success)
                return result;
        }

        return WorkspaceChangeResult.Successful();
    }
}