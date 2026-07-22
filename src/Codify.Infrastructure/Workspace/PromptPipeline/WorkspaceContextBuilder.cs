using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Workspace.Prompt;

namespace Codify.Infrastructure.Workspace.PromptPipeline
{
    /// <summary>
    /// Coordinates all workspace context providers and builds the final prompt context.
    /// </summary>
    public sealed class WorkspaceContextBuilder(
        IEnumerable<IWorkspaceContextOrchestrator> providers,
        IPromptContextComposer composer)
        : IWorkspaceContextBuilder
    {
        public async Task<PromptContext> BuildAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken)
        {
            var results = new List<ContextProviderResult>();

            foreach (var provider in providers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await provider.GetContextAsync(
                    request,
                    cancellationToken);

                if (result != null)
                {
                    results.Add(result);
                }
            }

            return composer.Compose(results);
        }
    }
}