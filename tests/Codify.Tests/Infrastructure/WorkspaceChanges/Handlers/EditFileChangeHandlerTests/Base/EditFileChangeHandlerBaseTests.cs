using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests.Base;

public abstract class EditFileChangeHandlerBaseTests
{
    protected IWorkspaceFileService WorkspaceFileService = null!;
    protected ITextChangeMatcher TextChangeMatcher = null!;
    protected IWorkspaceChangeErrorFactory WorkspaceChangeErrorFactory = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
        TextChangeMatcher = Substitute.For<ITextChangeMatcher>();
        WorkspaceChangeErrorFactory = Substitute.For<IWorkspaceChangeErrorFactory>();
    }

    protected virtual EditFileChangeHandler CreateSut()
    {
        return new EditFileChangeHandler(
            WorkspaceFileService,
            TextChangeMatcher,
            WorkspaceChangeErrorFactory);
    }
}