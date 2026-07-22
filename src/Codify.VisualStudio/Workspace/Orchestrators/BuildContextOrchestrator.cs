using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Infrastructure.Workspace.PromptPipeline;

namespace Codify.VisualStudio.Workspace.Orchestrators;

public sealed class BuildContextOrchestrator(
    IBuildContextProvider buildContextProvider,
    IBuildContextFormatter buildContextFormatter)
    : IWorkspaceContextOrchestrator
{
    public async Task<ContextProviderResult> GetContextAsync(
        WorkspaceContextRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.References?.Any(x => x.Type == ReferenceKind.Output) == true)
        {
            return new ContextProviderResult();
        }

        var context =
            await buildContextProvider.GetContextAsync(cancellationToken);

        if (context.Messages.Count == 0)
        {
            return new ContextProviderResult();
        }

        var result = new ContextProviderResult();

        result.Items.Add(
            PromptContextItemFactory.Create(
                PromptContextKind.Build,
                "Build",
                buildContextFormatter.Format(context)));

        return result;
    }
}