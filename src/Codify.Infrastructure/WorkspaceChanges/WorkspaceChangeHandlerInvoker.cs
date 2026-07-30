using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.WorkspaceChanges;

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