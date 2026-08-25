using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models.Workspace;
using Codinex.Core.Workspace.Prompt;
using Codinex.Infrastructure.Workspace.PromptPipeline;

namespace Codinex.VisualStudio.Workspace.Orchestrators;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
public sealed class GitContextOrchestrator(
    IGitContextProvider gitContextProvider,
    IGitContextFormatter gitContextFormatter)
    : IWorkspaceContextOrchestrator
{
    public string Name => "git_context";

    public string Description => "Provides the current Git branch and pending changes for the workspace.";

    public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Debug;

    public async Task<ContextProviderResult> GetContextAsync(
        WorkspaceContextRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = await gitContextProvider.GetContextAsync(cancellationToken);

        var result = new ContextProviderResult();

        result.Items.Add(
            PromptContextItemFactory.Create(
                PromptContextKind.Git,
                "Git",
                gitContextFormatter.Format(context)));

        return result;
    }
}
