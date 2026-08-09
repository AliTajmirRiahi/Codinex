using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Workspace)]
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

        WorkspaceChangeResult result = null;

        foreach (var workspaceChange in workspaceChangeSet.Changes)
        {
            result =
                await workspaceChangeHandlerInvoker.InvokeAsync(
                    workspaceChange,
                    cancellationToken);

            if (!result.Success)
                return result;
        }

        return result;
    }
}