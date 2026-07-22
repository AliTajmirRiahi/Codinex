using Codify.Core.Chat;
using Codify.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Builds the prompt context for the current chat session.
    /// </summary>
    public interface IWorkspaceContextBuilder
    {
        Task<PromptContext> BuildAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken);
    }
}