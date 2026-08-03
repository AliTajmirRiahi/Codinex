using System;
using Codify.Core.Interfaces;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.DeleteFileChangeHandlerTests.Base;

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