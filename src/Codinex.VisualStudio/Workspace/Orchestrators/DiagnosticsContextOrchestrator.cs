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
    /// Provides workspace diagnostics as prompt context.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class DiagnosticsContextOrchestrator(
        IDiagnosticsProvider diagnosticsProvider,
        IDiagnosticsFormatter diagnosticsFormatter)
        : IWorkspaceContextOrchestrator
    {
        public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Model;

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