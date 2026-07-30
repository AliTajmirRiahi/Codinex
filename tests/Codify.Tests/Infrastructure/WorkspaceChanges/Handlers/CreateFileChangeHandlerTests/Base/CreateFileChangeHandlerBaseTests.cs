using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests.Base;

public abstract class CreateFileChangeHandlerBaseTests
{
    protected IWorkspaceFileService WorkspaceFileService = null!;
    protected IWorkspaceChangeErrorFactory WorkspaceChangeErrorFactory = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
        WorkspaceChangeErrorFactory = Substitute.For<IWorkspaceChangeErrorFactory>();
    }

    protected virtual CreateFileChangeHandler CreateSut()
    {
        return new CreateFileChangeHandler(
            WorkspaceFileService,
            WorkspaceChangeErrorFactory);
    }
}