using Codify.Core.Interfaces;
using Codify.VisualStudio.Workspace.Orchestrators;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.VisualStudio.Workspace.Orchestrators.OpenDocumentsContextOrchestratorTests.Base;

public abstract class OpenDocumentsContextOrchestratorTestBase
{
    protected IOpenDocumentsProvider OpenDocumentsProvider = null!;
    protected IOpenDocumentsFormatter OpenDocumentsFormatter = null!;

    [SetUp]
    public virtual void SetUp()
    {
        OpenDocumentsProvider = Substitute.For<IOpenDocumentsProvider>();
        OpenDocumentsFormatter = Substitute.For<IOpenDocumentsFormatter>();
    }

    protected OpenDocumentsContextOrchestrator CreateSut()
    {
        return new OpenDocumentsContextOrchestrator(
            OpenDocumentsProvider,
            OpenDocumentsFormatter);
    }
}