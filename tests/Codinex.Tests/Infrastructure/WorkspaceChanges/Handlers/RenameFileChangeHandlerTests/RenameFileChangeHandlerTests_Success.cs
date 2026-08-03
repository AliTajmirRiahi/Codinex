using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests.Base;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests;

[TestFixture]
public sealed class RenameFileChangeHandlerTests_Success
    : RenameFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldRenameFileAsync()
    {
        // Arrange
        var change = new RenameFileChange
        {
            FilePath = @"C:\Test\UserService.cs",
            NewFileName = "AccountService.cs"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1).RenameAsync(
            change.FilePath,
            change.NewFileName,
            Arg.Any<CancellationToken>());

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Test]
    public async Task HandleAsync_ShouldPassCancellationTokenAsync()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        var change = new RenameFileChange
        {
            FilePath = @"C:\Test\UserService.cs",
            NewFileName = "AccountService.cs"
        };

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            change,
            cancellationToken);

        // Assert
        await WorkspaceFileService.Received(1).RenameAsync(
            change.FilePath,
            change.NewFileName,
            cancellationToken);
    }

    [Test]
    public async Task HandleAsync_ShouldReturnSuccessfulResultAsync()
    {
        // Arrange
        var change = new RenameFileChange
        {
            FilePath = @"C:\Test\UserService.cs",
            NewFileName = "AccountService.cs"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }
}