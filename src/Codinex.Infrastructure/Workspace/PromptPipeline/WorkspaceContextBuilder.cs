using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Infrastructure.Workspace.PromptPipeline
{
    /// <summary>
    /// Coordinates all workspace context providers and builds the final prompt context.
    /// </summary>
    [AutoDiRegister(Modules.Prompt, RegistrationOrder.Infrastructure)]
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

            foreach (var provider in providers.Where(
                         p => p.Visibility == WorkspaceContextVisibility.Model)
                     )
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