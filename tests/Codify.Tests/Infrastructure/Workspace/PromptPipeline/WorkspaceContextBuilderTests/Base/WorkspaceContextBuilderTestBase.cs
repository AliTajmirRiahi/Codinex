using System.Collections.Generic;
using Codify.Core.Workspace.Prompt;
using Codify.Infrastructure.Workspace.PromptPipeline;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.WorkspaceContextBuilderTests.Base;

public abstract class WorkspaceContextBuilderTestBase
{
    protected IPromptContextComposer Composer = null!;

    protected IWorkspaceContextOrchestrator Provider1 = null!;
    protected IWorkspaceContextOrchestrator Provider2 = null!;
    protected IWorkspaceContextOrchestrator Provider3 = null!;

    [SetUp]
    public virtual void SetUp()
    {
        Composer = Substitute.For<IPromptContextComposer>();

        Provider1 = Substitute.For<IWorkspaceContextOrchestrator>();
        Provider2 = Substitute.For<IWorkspaceContextOrchestrator>();
        Provider3 = Substitute.For<IWorkspaceContextOrchestrator>();
    }

    protected WorkspaceContextBuilder CreateSut(
        params IWorkspaceContextOrchestrator[] providers)
    {
        return new WorkspaceContextBuilder(
            providers,
            Composer);
    }

    protected static WorkspaceContextRequest CreateRequest()
    {
        return new WorkspaceContextRequest();
    }
}