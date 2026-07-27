using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Infrastructure.Workspace.PromptPipeline;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Workspace.Orchestrators
{
    /// <summary>
    /// Provides workspace diagnostics as prompt context.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class DiagnosticsContextOrchestrator(
        IDiagnosticsProvider diagnosticsProvider,
        IDiagnosticsFormatter diagnosticsFormatter)
        : IWorkspaceContextOrchestrator
    {
        public async Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var diagnostics =
                await diagnosticsProvider.GetDiagnosticsAsync(
                    DiagnosticsScope.Solution,
                    cancellationToken);

            if (diagnostics == null || diagnostics.Count == 0)
            {
                return new ContextProviderResult();
            }

            var result = new ContextProviderResult();

            result.Items.Add(
                PromptContextItemFactory.Create(
                    PromptContextKind.Diagnostics,
                    "Diagnostics",
                    diagnosticsFormatter.Format(diagnostics)));

            return result;
        }
    }
}