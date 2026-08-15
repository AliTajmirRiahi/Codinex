using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;
using Codinex.Infrastructure.Workspace.PromptPipeline;

namespace Codinex.VisualStudio.Workspace.Orchestrators
{
    /// <summary>
    /// Provides the active document as workspace context.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class CurrentDocumentContextOrchestrator(
        IActiveDocumentProvider activeDocumentProvider,
        IReferenceContextFormatter referenceContextFormatter)
        : IWorkspaceContextOrchestrator
    {
        public string Name => "current_document_context";

        public string Description => "Provides the active document as workspace context.";

        public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Debug;

        public async Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentReference = await activeDocumentProvider.GetActiveDocumentAsync();

            if (currentReference == null)
            {
                return new ContextProviderResult();
            }

            // Skip automatic context when the active document
            // has already been selected explicitly by the user.
            if (request.References != null &&
                request.References.Any(r =>
                    r.Type == currentReference.Type &&
                    string.Equals(
                        r.Value,
                        currentReference.Value,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return new ContextProviderResult();
            }

            var result = new ContextProviderResult();

            result.Items.Add(
                PromptContextItemFactory.Create(
                    PromptContextKind.CurrentDocument,
                    currentReference.Name,
                    referenceContextFormatter.Format(currentReference)));

            return result;
        }
    }
}