using System;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.DeleteFileChangeHandlerTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.DeleteFileChangeHandlerTests;

public sealed class DeleteFileChangeHandlerTests_Failure
    : DeleteFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldReturnFailure_WhenFileDoesNotExistAsync()
    {
        var change = CreateChange();

        WorkspaceFileService.Exists(change.FilePath)
            .Returns(false);

        var error = new WorkspaceChangeError();

        ErrorFactory.Create(
                WorkspaceChangeErrorCode.FileNotFound,
                change.FilePath,
                change.Id,
                Arg.Any<string>())
            .Returns(error);

        var result = await Sut.HandleAsync(change);

        result.Success.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
    }

    [Test]
    public async Task HandleAsync_ShouldNotDeleteFile_WhenFileDoesNotExistAsync()
    {
        var change = CreateChange();

        WorkspaceFileService.Exists(change.FilePath)
            .Returns(false);

        ErrorFactory.Create(
                Arg.Any<WorkspaceChangeErrorCode>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string>())
            .Returns(new WorkspaceChangeError());

        await Sut.HandleAsync(change);

        await WorkspaceFileService
            .DidNotReceive()
            .DeleteAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldCreateWorkspaceChangeError_WhenFileDoesNotExistAsync()
    {
        var change = CreateChange();

        WorkspaceFileService.Exists(change.FilePath)
            .Returns(false);

        ErrorFactory.Create(
                Arg.Any<WorkspaceChangeErrorCode>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string>())
            .Returns(new WorkspaceChangeError());

        await Sut.HandleAsync(change);

        ErrorFactory
            .Received(1)
            .Create(
                WorkspaceChangeErrorCode.FileNotFound,
                change.FilePath,
                change.Id,
                Arg.Any<string>());
    }
}