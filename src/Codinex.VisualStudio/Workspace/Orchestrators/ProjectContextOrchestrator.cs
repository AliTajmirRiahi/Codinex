using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models.Workspace;
using Codinex.Core.Workspace.Prompt;
using Codinex.Infrastructure.Workspace.PromptPipeline;

namespace Codinex.VisualStudio.Workspace.Orchestrators
{
    /// <summary>
    /// Provides project information as workspace context.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class ProjectContextOrchestrator(
        IProjectContextProvider projectContextProvider,
        IProjectContextFormatter projectContextFormatter)
        : IWorkspaceContextOrchestrator
    {
        public string Name => "project_context";

        public string Description => "Provides project information as workspace context.";

        public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Debug;

        public async Task<ContextProviderResult> GetContextAsync(
            WorkspaceContextRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectContext =
                await projectContextProvider.GetContextAsync(cancellationToken);

            if (projectContext?.Projects == null ||
                projectContext.Projects.Count == 0)
            {
                return new ContextProviderResult();
            }

            var result = new ContextProviderResult();

            result.Items.Add(
                PromptContextItemFactory.Create(
                    PromptContextKind.Project,
                    "Project",
                    projectContextFormatter.Format(projectContext)));

            return result;
        }
    }
}