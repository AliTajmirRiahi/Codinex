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
    /// Provides workspace memory as prompt context.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class MemoryContextOrchestrator(
        IMemoryContextProvider memoryContextProvider,
        IMemoryContextFormatter memoryFormatter)
        : IWorkspaceContextOrchestrator
    {
        public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Model;

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