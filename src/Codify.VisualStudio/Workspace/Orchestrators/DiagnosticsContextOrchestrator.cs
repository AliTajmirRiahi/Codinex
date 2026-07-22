using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Infrastructure.Workspace.PromptPipeline;

namespace Codify.VisualStudio.Workspace.Orchestrators
{
    /// <summary>
    /// Provides workspace diagnostics as prompt context.
    /// </summary>
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