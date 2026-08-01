using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Infrastructure.Workspace.PromptPipeline;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Workspace.Orchestrators
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