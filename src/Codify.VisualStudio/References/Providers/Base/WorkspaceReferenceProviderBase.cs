using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Codify.VisualStudio.References.Providers.Base
{
    /// <summary>
    /// Base class for providers that iterate through projects in the current workspace.
    /// </summary>
    public abstract class WorkspaceReferenceProviderBase(IVisualStudioServices visualStudio)
        : VsServiceBase(visualStudio), IReferenceProvider
    {
        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (await GetWorkspaceAsync() is not { } workspace || await GetDteAsync() is not { Solution: not null })
                return Array.Empty<ReferenceItem>();

            var result = new List<ReferenceItem>();

            foreach (var project in workspace.CurrentSolution.Projects)
            {
                var references = await ExtractReferencesAsync(project)
                    .ConfigureAwait(false);

                if (references.Count > 0)
                    result.AddRange(references);
            }

            return result;
        }

        protected abstract Task<IReadOnlyList<ReferenceItem>>
            ExtractReferencesAsync(Project project);
    }
}