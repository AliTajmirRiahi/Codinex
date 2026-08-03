using Codinex.VisualStudio.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests.Base;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests;

[TestFixture]
public sealed class CreateFileChangeHandlerTests_Success
    : CreateFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldCreateFileAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";
        const string content = "Hello Codify";

        var change = new CreateFileChange
        {
            FilePath = filePath,
            Content = content
        };

        WorkspaceFileService
            .Exists(filePath)
            .Returns(false);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();

        WorkspaceFileService.Received(1)
            .Exists(filePath);

        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                content,
                Arg.Any<System.Text.Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldWriteSpecifiedContentAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";
        const string content = "public class Test {}";

        var change = new CreateFileChange
        {
            FilePath = filePath,
            Content = content
        };

        WorkspaceFileService
            .Exists(filePath)
            .Returns(false);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                content,
                Arg.Any<System.Text.Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldReturnSuccessfulResultAsync()
    {
        // Arrange
        var change = new CreateFileChange
        {
            FilePath = @"C:\Test\File.cs",
            Content = "Hello"
        };

        WorkspaceFileService
            .Exists(change.FilePath)
            .Returns(false);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Test]
    public async Task HandleAsync_ShouldPassCancellationTokenToWriteAsync()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        var change = new CreateFileChange
        {
            FilePath = @"C:\Test\File.cs",
            Content = "Hello"
        };

        WorkspaceFileService
            .Exists(change.FilePath)
            .Returns(false);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            change,
            cancellationToken);

        // Assert
        await WorkspaceFileService.Received(1)
            .WriteAsync(
                change.FilePath,
                change.Content,
                Arg.Any<System.Text.Encoding>(),
                cancellationToken);
    }
}