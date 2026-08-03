using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests.Base;

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