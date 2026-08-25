using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Workspace;
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

            var modelProviders = GetModelProviders(request).ToList();

            foreach (var provider in modelProviders)
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

        public IReadOnlyList<AiPreprocessorCatalogItem> GetAvailableContexts()
        {
            return providers
                .Where(p => p.Visibility == WorkspaceContextVisibility.Model)
                .Select(p => new AiPreprocessorCatalogItem
                {
                    Name = p.Name,
                    Description = p.Description
                })
                .ToList();
        }

        private IEnumerable<IWorkspaceContextOrchestrator> GetModelProviders(WorkspaceContextRequest request)
        {
            var modelProviders = providers.Where(p => p.Visibility == WorkspaceContextVisibility.Model);

            if (request?.ContextsNeeded == null)
            {
                return modelProviders;
            }

            var contextsNeeded = new HashSet<string>(
                request.ContextsNeeded.Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);

            return modelProviders.Where(p => contextsNeeded.Contains(p.Name));
        }
    }
}