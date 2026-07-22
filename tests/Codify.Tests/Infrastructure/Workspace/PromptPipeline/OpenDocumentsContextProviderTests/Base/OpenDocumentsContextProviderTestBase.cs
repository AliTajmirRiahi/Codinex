using Codify.Core.Interfaces;
using Codify.VisualStudio.Workspace.Providers;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.OpenDocumentsContextProviderTests.Base;

public abstract class OpenDocumentsContextProviderTestBase
{
    protected IOpenDocumentsContextProvider OpenDocumentsContextProvider = null!;
    protected IOpenDocumentsFormatter OpenDocumentsFormatter = null!;

    [SetUp]
    public virtual void SetUp()
    {
        OpenDocumentsContextProvider = Substitute.For<IOpenDocumentsContextProvider>();
        OpenDocumentsFormatter = Substitute.For<IOpenDocumentsFormatter>();
    }

    protected OpenDocumentsContextOrchestrator CreateSut()
    {
        return new OpenDocumentsContextOrchestrator(
            OpenDocumentsContextProvider,
            OpenDocumentsFormatter);
    }
}