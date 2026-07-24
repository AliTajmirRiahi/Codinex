using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Infrastructure.Workspace.PromptPipeline;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Workspace.Orchestrators
{
    /// <summary>
    /// Provides information about the currently open documents.
    /// </summary>
    public sealed class OpenDocumentsContextOrchestrator(
        IOpenDocumentsProvider openDocumentsProvider,
        IOpenDocumentsFormatter openDocumentsFormatter)
        : IWorkspaceContextOrchestrator
    {
        public async Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var documents = await openDocumentsProvider
                .GetOpenDocumentsAsync(cancellationToken);

            if (documents == null ||
                documents.Count == 0)
            {
                return new ContextProviderResult();
            }

            // Skip automatic context when the user explicitly attached open documents.
            if (request.References?.Any(x => x.Type == ReferenceKind.OpenDocuments) == true)
            {
                return new ContextProviderResult();
            }

            var result = new ContextProviderResult();

            result.Items.Add(
                PromptContextItemFactory.Create(
                    PromptContextKind.OpenDocuments,
                    "Open Documents",
                    openDocumentsFormatter.Format(documents)));

            return result;
        }
    }
}