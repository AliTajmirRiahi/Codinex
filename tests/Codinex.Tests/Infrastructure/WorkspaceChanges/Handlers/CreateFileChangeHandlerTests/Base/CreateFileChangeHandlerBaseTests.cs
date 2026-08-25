using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests.Base;

public abstract class CreateFileChangeHandlerBaseTests
{
    protected IWorkspaceFileService WorkspaceFileService = null!;
    protected IWorkspaceChangeErrorFactory WorkspaceChangeErrorFactory = null!;
    protected IWorkspaceChangeHandler<CreateDirectoryChange> createDirectoryChangeHandler = null!;
    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
        WorkspaceChangeErrorFactory = Substitute.For<IWorkspaceChangeErrorFactory>();
        createDirectoryChangeHandler = Substitute.For<IWorkspaceChangeHandler<CreateDirectoryChange>>();
    }

    protected virtual CreateFileChangeHandler CreateSut()
    {
        return new CreateFileChangeHandler(
            WorkspaceFileService,
            WorkspaceChangeErrorFactory,
            createDirectoryChangeHandler);
    }
}