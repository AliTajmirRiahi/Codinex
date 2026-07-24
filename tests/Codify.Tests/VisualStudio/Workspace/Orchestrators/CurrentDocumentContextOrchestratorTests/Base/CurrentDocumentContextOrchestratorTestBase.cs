using Codify.Core.Interfaces;
using Codify.Core.Workspace.Prompt;
using Codify.VisualStudio.Workspace.Orchestrators;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.VisualStudio.Workspace.Orchestrators.CurrentDocumentContextOrchestratorTests.Base;

public abstract class CurrentDocumentContextOrchestratorTestBase
{
    protected IActiveDocumentProvider ActiveDocumentProvider = null!;
    protected IReferenceContextFormatter ReferenceContextFormatter = null!;

    [SetUp]
    public virtual void SetUp()
    {
        ActiveDocumentProvider = Substitute.For<IActiveDocumentProvider>();
        ReferenceContextFormatter = Substitute.For<IReferenceContextFormatter>();
    }

    /// <summary>
    /// Creates the system under test.
    /// </summary>
    protected virtual CurrentDocumentContextOrchestrator CreateSut()
    {
        return new CurrentDocumentContextOrchestrator(
            ActiveDocumentProvider,
            ReferenceContextFormatter);
    }

    protected static WorkspaceContextRequest CreateRequest()
    {
        return new WorkspaceContextRequest();
    }
}