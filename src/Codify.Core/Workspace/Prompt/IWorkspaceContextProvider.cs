using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Produces prompt context for a specific source.
    /// </summary>
    public interface IWorkspaceContextOrchestrator
    {
        Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken);
    }
}