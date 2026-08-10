using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Workspace;

/// <summary>
/// Restores a changeset review left pending for the current solution from a previous session
/// (tool window closed, or Visual Studio itself closed, before the user decided).
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
public sealed class PendingChangesetRestoreStartupTask(IChangesetSessionService changesetSessionService) : IStartupTask
{
    public Task StartAsync() => changesetSessionService.TryRestorePendingReviewAsync(CancellationToken.None);
}
