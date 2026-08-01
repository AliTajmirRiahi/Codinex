using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using System;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;

namespace Codify.Infrastructure.WorkspaceChanges;

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

            var tt = Newtonsoft.Json.JsonConvert.SerializeObject(workspaceChange);

            if (!result.Success)
                return result;
        }

        return result;
    }
}