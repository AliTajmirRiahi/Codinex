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
    /// Provides project information as workspace context.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Features)]
    public sealed class ProjectContextOrchestrator(
        IProjectContextProvider projectContextProvider,
        IProjectContextFormatter projectContextFormatter)
        : IWorkspaceContextOrchestrator
    {
        public WorkspaceContextVisibility Visibility { get; } = WorkspaceContextVisibility.Model;

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