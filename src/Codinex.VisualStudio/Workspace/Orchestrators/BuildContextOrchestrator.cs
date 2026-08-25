using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;
using Codinex.Infrastructure.Workspace.PromptPipeline;

namespace Codinex.VisualStudio.Workspace.Orchestrators;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
public sealed class BuildContextOrchestrator(
    IBuildContextProvider buildContextProvider,
    IBuildContextFormatter buildContextFormatter)
    : IWorkspaceContextOrchestrator
{
    public string Name => "build_context";

    public string Description => "Provides build output and related build context for the workspace.";

    public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Debug;

    public async Task<ContextProviderResult> GetContextAsync(
        WorkspaceContextRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.References?.Any(x => x.Type == ReferenceKind.Output) == true)
        {
            return new ContextProviderResult();
        }

        var context = await buildContextProvider.GetContextAsync(cancellationToken);

        var result = new ContextProviderResult();

        result.Items.Add(
            PromptContextItemFactory.Create(
                PromptContextKind.Build,
                "Build",
                buildContextFormatter.Format(context)));

        return result;
    }
}