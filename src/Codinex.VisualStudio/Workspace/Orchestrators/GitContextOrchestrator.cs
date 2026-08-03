//using System.Threading;
//using System.Threading.Tasks;
//using Codinex.Core.Interfaces;
//using Codinex.Core.Workspace.Prompt;
//using Codinex.Infrastructure.Workspace.PromptPipeline;

//namespace Codinex.VisualStudio.Workspace.Orchestrators
//{
//    /// <summary>
//    /// Provides Git information as workspace context.
//    /// </summary>
//    public sealed class GitContextOrchestrator(
//        IGitContextProvider gitContextProvider,
//        IGitContextFormatter gitContextFormatter)
//        : IWorkspaceContextOrchestrator
//    {
//        public async Task<ContextProviderResult> GetContextAsync(
//            WorkspaceContextRequest request,
//            CancellationToken cancellationToken)
//        {
//            cancellationToken.ThrowIfCancellationRequested();

//            var gitContext =
//                await gitContextProvider.GetContextAsync(cancellationToken);

//            if (gitContext?.Files == null ||
//                gitContext.Files.Count == 0)
//            {
//                return new ContextProviderResult();
//            }

//            var result = new ContextProviderResult();

//            result.Items.Add(
//                PromptContextItemFactory.Create(
//                    PromptContextKind.Git,
//                    "Git",
//                    gitContextFormatter.Format(gitContext)));

//            return result;
//        }
//    }
//}