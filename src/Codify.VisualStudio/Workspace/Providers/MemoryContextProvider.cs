using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;

namespace Codify.VisualStudio.Workspace.Providers;

/// <summary>
/// Provides long-term workspace memory.
/// </summary>
public sealed class MemoryContextProvider : IWorkspaceContextOrchestrator
{
    public Task<ContextProviderResult> GetContextAsync(
        WorkspaceContextRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new ContextProviderResult());
    }
}