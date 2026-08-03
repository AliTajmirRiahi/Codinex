using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Workspace.Prompt
{
    public enum WorkspaceContextVisibility
    {
        Model = 0,

        Internal = 1,

        Debug = 2,

        Experimental = 3,
    }
    /// <summary>
    /// Produces prompt context for a specific source.
    /// </summary>
    public interface IWorkspaceContextOrchestrator
    {
        WorkspaceContextVisibility Visibility { get; }

        Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken);
    }
}