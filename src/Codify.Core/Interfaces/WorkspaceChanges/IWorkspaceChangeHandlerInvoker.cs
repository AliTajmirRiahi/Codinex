using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;

namespace Codify.Core.Interfaces.WorkspaceChanges;

public interface IWorkspaceChangeHandlerInvoker
{
    Task<WorkspaceChangeResult> InvokeAsync(
        WorkspaceChange workspaceChange,
        CancellationToken cancellationToken = default);
}