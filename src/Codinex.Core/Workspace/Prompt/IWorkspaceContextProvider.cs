using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Workspace.Prompt
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
        /// <summary>
        /// Gets the unique workspace context orchestrator name.
        /// </summary>
        string Name { get; }

        string Description { get; }

        WorkspaceContextVisibility Visibility { get; }

        Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken);
    }
}