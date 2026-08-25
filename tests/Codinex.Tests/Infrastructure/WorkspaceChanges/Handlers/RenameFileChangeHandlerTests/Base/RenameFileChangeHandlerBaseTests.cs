using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests.Base;

public abstract class RenameFileChangeHandlerBaseTests
{
    protected IWorkspaceFileService WorkspaceFileService = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
    }

    protected virtual RenameFileChangeHandler CreateSut()
    {
        return new RenameFileChangeHandler(
            WorkspaceFileService);
    }
}