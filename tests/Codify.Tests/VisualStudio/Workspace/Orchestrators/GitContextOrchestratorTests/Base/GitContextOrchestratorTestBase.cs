using Codify.Core.Interfaces;
using Codify.VisualStudio.Workspace.Orchestrators;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.VisualStudio.Workspace.Orchestrators.GitContextOrchestratorTests.Base;

public abstract class GitContextOrchestratorTestBase
{
    protected IGitContextProvider GitContextProvider = null!;
    protected IGitContextFormatter GitContextFormatter = null!;

    [SetUp]
    public virtual void SetUp()
    {
        GitContextProvider = Substitute.For<IGitContextProvider>();
        GitContextFormatter = Substitute.For<IGitContextFormatter>();
    }

    //protected GitContextOrchestrator CreateSut()
    //{
    //    return new GitContextOrchestrator(
    //        GitContextProvider,
    //        GitContextFormatter);
    //}
}