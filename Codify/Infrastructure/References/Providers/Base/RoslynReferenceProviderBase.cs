using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Infrastructure.VisualStudio.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Codify.Infrastructure.References.Providers.Base
{
    public abstract class RoslynReferenceProviderBase
        : VsServiceBase, IReferenceProvider
    {
        private readonly IVisualStudioServices _visualStudio;

        protected RoslynReferenceProviderBase(IVisualStudioServices visualStudio)
            : base(visualStudio)
        {
            _visualStudio = visualStudio;
        }

        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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

        protected virtual bool IsSupportedDocument(Document document)
        {
            return document.FilePath?.EndsWith(".cs",
                StringComparison.OrdinalIgnoreCase) == true;
        }

        protected abstract Task<IReadOnlyList<ReferenceItem>>
            ExtractReferencesAsync(
                Project project,
                Document document);
    }
}
