using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.Workspace;

namespace Codinex.Core.Workspace.Prompt
{
    public sealed class AiPreprocessorCatalogItem
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IReadOnlyList<string> Capabilities { get; set; } = [];
    }

    /// <summary>
    /// Builds the prompt context for the current chat session.
    /// </summary>
    public interface IWorkspaceContextBuilder
    {
        Task<PromptContext> BuildAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken);

        IReadOnlyList<AiPreprocessorCatalogItem> GetAvailableContexts();
    }
}