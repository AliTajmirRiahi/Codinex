using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Infrastructure.VisualStudio.Internal;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Codify.Infrastructure.References.Providers.Base
{
    /// <summary>
    /// Base class for providers that iterate through projects in the current workspace.
    /// </summary>
    public abstract class WorkspaceReferenceProviderBase
        : VsServiceBase, IReferenceProvider
    {
        protected WorkspaceReferenceProviderBase(IVisualStudioServices visualStudio)
            : base(visualStudio)
        {
        }

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