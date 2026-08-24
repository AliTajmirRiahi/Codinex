using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Codinex.VisualStudio.References.Providers.Base
{
    public abstract class RoslynReferenceProviderBase(
        IVisualStudioServices visualStudio,
        IUiThreadDispatcher uiThreadDispatcher,
        IWorkspaceIgnoreService workspaceIgnoreService)
        : VsServiceBase(visualStudio), IReferenceProvider
    {
        private readonly IVisualStudioServices _visualStudio = visualStudio;
        private readonly IUiThreadDispatcher _uiThreadDispatcher = uiThreadDispatcher;
        private readonly IWorkspaceIgnoreService _workspaceIgnoreService = workspaceIgnoreService;

        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            await _uiThreadDispatcher.SwitchToMainThreadAsync();

            if (await GetWorkspaceAsync() is not { } workspace || await GetDteAsync() is not { Solution: not null })
                return Array.Empty<ReferenceItem>();

            var result = new List<ReferenceItem>();

            foreach (var project in workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (!IsSupportedDocument(document))
                        continue;

                    result.AddRange(
                        await ExtractReferencesAsync(project, document)
                            .ConfigureAwait(false));
                }
            }

            return result;
        }

        /// <summary>
        /// Re-extracts references for a single document, without traversing the whole solution.
        /// Used by <see cref="ISymbolReferenceWatcher"/> to refresh just the document that changed.
        /// </summary>
        public Task<IReadOnlyList<ReferenceItem>> GetReferencesForDocumentAsync(Document document)
        {
            if (document == null || !IsSupportedDocument(document))
                return Task.FromResult<IReadOnlyList<ReferenceItem>>(Array.Empty<ReferenceItem>());

            return ExtractReferencesAsync(document.Project, document);
        }

        protected virtual bool IsSupportedDocument(Document document)
        {
            if (document.FilePath?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) != true)
                return false;

            // Skip generated/build-output documents (e.g. under obj/, node_modules/) that Roslyn's
            // project system sometimes still includes, so large solutions don't spend time parsing
            // syntax trees that were never useful as references anyway.
            return !_workspaceIgnoreService.ShouldIgnore(document.FilePath);
        }

        protected abstract Task<IReadOnlyList<ReferenceItem>>
            ExtractReferencesAsync(
                Project project,
                Document document);
    }
}
