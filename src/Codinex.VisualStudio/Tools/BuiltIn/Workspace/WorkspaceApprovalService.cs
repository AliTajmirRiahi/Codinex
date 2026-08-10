using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class WorkspaceApprovalService : IWorkspaceApprovalService
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _pendingDecisions = new();

    public Task<bool> WaitForApprovalAsync(
        Guid changesetId,
        CancellationToken cancellationToken)
    {
        var tcs = _pendingDecisions.GetOrAdd(
            changesetId,
            _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

        cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled(cancellationToken);
            _pendingDecisions.TryRemove(changesetId, out _);
        });

        return tcs.Task;
    }

    public void SetDecision(Guid changesetId, bool approved)
    {
        if (_pendingDecisions.TryRemove(changesetId, out var tcs))
        {
            tcs.TrySetResult(approved);
        }
    }
}
