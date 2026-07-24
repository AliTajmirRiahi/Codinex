//using System.Threading;
//using System.Threading.Tasks;
//using Codify.Core.Interfaces;
//using Codify.Core.Workspace.Prompt;
//using Codify.Infrastructure.Workspace.PromptPipeline;

//namespace Codify.VisualStudio.Workspace.Orchestrators
//{
//    /// <summary>
//    /// Provides project information as workspace context.
//    /// </summary>
//    public sealed class ProjectContextOrchestrator(
//        IProjectContextProvider projectContextProvider,
//        IProjectContextFormatter projectContextFormatter)
//        : IWorkspaceContextOrchestrator
//    {
//        public async Task<ContextProviderResult> GetContextAsync(
//            WorkspaceContextRequest request,
//            CancellationToken cancellationToken)
//        {
//            cancellationToken.ThrowIfCancellationRequested();

//            var projectContext =
//                await projectContextProvider.GetContextAsync(cancellationToken);

//            if (projectContext?.Projects == null ||
//                projectContext.Projects.Count == 0)
//            {
//                return new ContextProviderResult();
//            }

//            var result = new ContextProviderResult();

//            result.Items.Add(
//                PromptContextItemFactory.Create(
//                    PromptContextKind.Project,
//                    "Project",
//                    projectContextFormatter.Format(projectContext)));

//            return result;
//        }
//    }
//}