using Codinex.Core.Chat;
using Codinex.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Workspace.Prompt
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