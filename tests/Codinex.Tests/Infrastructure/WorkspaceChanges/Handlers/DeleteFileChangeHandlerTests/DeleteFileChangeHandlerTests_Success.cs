using System.Threading;
using System.Threading.Tasks;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.DeleteFileChangeHandlerTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.DeleteFileChangeHandlerTests;

public sealed class DeleteFileChangeHandlerTests_Success
    : DeleteFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldDeleteFileAsync()
    {
        var change = CreateChange();

        WorkspaceFileService.Exists(change.FilePath)
            .Returns(true);

        await Sut.HandleAsync(change);

        await WorkspaceFileService
            .Received(1)
            .DeleteAsync(change.FilePath, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldReturnSuccessfulResultAsync()
    {
        var change = CreateChange();

        WorkspaceFileService.Exists(change.FilePath)
            .Returns(true);

        var result = await Sut.HandleAsync(change);

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Test]
    public async Task HandleAsync_ShouldPassCancellationTokenToDeleteAsync()
    {
        var change = CreateChange();

        WorkspaceFileService.Exists(change.FilePath)
            .Returns(true);

        using var cancellation = new CancellationTokenSource();

        await Sut.HandleAsync(change, cancellation.Token);

        await WorkspaceFileService
            .Received(1)
            .DeleteAsync(change.FilePath, cancellation.Token);
    }
}