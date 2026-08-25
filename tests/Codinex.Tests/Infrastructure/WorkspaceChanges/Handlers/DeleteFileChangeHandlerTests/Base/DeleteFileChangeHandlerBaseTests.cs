using System;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.DeleteFileChangeHandlerTests.Base;

public abstract class DeleteFileChangeHandlerBaseTests
{
    protected IWorkspaceFileService WorkspaceFileService = null!;
    protected IWorkspaceChangeErrorFactory ErrorFactory = null!;

    protected DeleteFileChangeHandler Sut = null!;

    [SetUp]
    public void SetUp()
    {
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
        ErrorFactory = Substitute.For<IWorkspaceChangeErrorFactory>();

        Sut = CreateSut();
    }

    protected DeleteFileChangeHandler CreateSut()
    {
        return new DeleteFileChangeHandler(
            WorkspaceFileService,
            ErrorFactory);
    }

    protected static DeleteFileChange CreateChange()
    {
        return new DeleteFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = @"C:\Temp\Test.cs"
        };
    }
}