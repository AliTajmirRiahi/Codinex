using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class WorkspaceChangeHandlerInvoker(
    IWorkspaceChangeHandlerResolver workspaceChangeHandlerResolver)
    : IWorkspaceChangeHandlerInvoker
{
    private static readonly MethodInfo ResolveMethod =
        typeof(IWorkspaceChangeHandlerResolver)
            .GetMethod(nameof(IWorkspaceChangeHandlerResolver.Resolve))!;

    public async Task<WorkspaceChangeResult> InvokeAsync(
        WorkspaceChange workspaceChange,
        CancellationToken cancellationToken = default)
    {
        if (workspaceChange == null)
            throw new ArgumentNullException(nameof(workspaceChange));

        var genericResolveMethod =
            ResolveMethod.MakeGenericMethod(workspaceChange.GetType());

        dynamic handler =
            genericResolveMethod.Invoke(
                workspaceChangeHandlerResolver,
                null)!;

        try
        {
            return await handler.HandleAsync(
                (dynamic)workspaceChange,
                cancellationToken);
        }
        catch (RuntimeBinderException ex)
        {
            throw new InvalidOperationException(
                $"Failed to invoke handler for workspace change '{workspaceChange.GetType().FullName}'.",
                ex);
        }
    }
}