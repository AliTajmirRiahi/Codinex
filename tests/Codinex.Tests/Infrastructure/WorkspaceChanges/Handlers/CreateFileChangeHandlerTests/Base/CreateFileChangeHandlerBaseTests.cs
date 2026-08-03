using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests.Base;

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