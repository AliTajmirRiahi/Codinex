using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models.References;
using Codinex.Core.Models.Workspace;
using Codinex.Core.Workspace.Prompt;
using Codinex.Infrastructure.Workspace.PromptPipeline;

namespace Codinex.VisualStudio.Workspace.Orchestrators
{
    /// <summary>
    /// Provides information about the currently open documents.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class OpenDocumentsContextOrchestrator(
        IOpenDocumentsProvider openDocumentsProvider,
        IOpenDocumentsFormatter openDocumentsFormatter)
        : IWorkspaceContextOrchestrator
    {
        public string Name => "open_documents_context";

        public string Description => "Provides information about the currently open documents.";

        public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Debug;

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