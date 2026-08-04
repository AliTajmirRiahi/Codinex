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
    /// Provides workspace memory as prompt context.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class MemoryContextOrchestrator(
        IMemoryContextProvider memoryContextProvider,
        IMemoryContextFormatter memoryFormatter)
        : IWorkspaceContextOrchestrator
    {
        public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Debug;

        public async Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            var context = memoryContextProvider.GetContext();

            var formatted = memoryFormatter.Format(context);

            if (string.IsNullOrWhiteSpace(formatted))
            {
                return new ContextProviderResult();
            }

            var result = new ContextProviderResult();

            result.Items.Add(
                PromptContextItemFactory.Create(
                    PromptContextKind.Memory,
                    "Workspace Memory",
                    formatted));

            return result;
        }
    }
}