using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests.Base;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests;

[TestFixture]
public sealed class EditFileChangeHandlerTests_EdgeCases
    : EditFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldApplyTextChangesInOrder_WhenTextChangesAreNotOrderedAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var second = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 2,
            Search = "World",
            Replace = "Codify"
        };

        var first = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "Hello",
            Replace = "Hi"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [second, first]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        TextChangeMatcher
            .Match("Hello World", first)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 0,
                Length = 5
            });

        TextChangeMatcher
            .Match("Hi World", second)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 3,
                Length = 5
            });

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            TextChangeMatcher.Match("Hello World", first);
            TextChangeMatcher.Match("Hi World", second);
        });
    }

    [Test]
    public async Task HandleAsync_ShouldNotCallMatcher_WhenTextChangesCollectionIsEmptyAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = []
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        TextChangeMatcher
            .DidNotReceive()
            .Match(Arg.Any<string>(), Arg.Any<TextFileChange>());
    }

    [Test]
    public async Task HandleAsync_ShouldWriteOriginalContent_WhenTextChangesCollectionIsEmptyAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";
        const string content = "Hello World";

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = []
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns(content);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                content,
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldSupportReplacingWithEmptyStringAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "World",
            Replace = string.Empty
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        TextChangeMatcher
            .Match("Hello World", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 6,
                Length = 5
            });

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                "Hello ",
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldSupportReplacingEntireFileContentAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "Hello World",
            Replace = "Codify"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        TextChangeMatcher
            .Match("Hello World", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 0,
                Length = 11
            });

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                "Codify",
                cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldPassCancellationTokenToReadAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var cancellationToken = new CancellationTokenSource().Token;

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = []
        };

        WorkspaceFileService
            .ReadAsync(filePath, cancellationToken)
            .Returns("Content");

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, cancellationToken);

        // Assert
        await WorkspaceFileService.Received(1)
            .ReadAsync(filePath, cancellationToken);
    }

    [Test]
    public async Task HandleAsync_ShouldPassCancellationTokenToWriteAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var cancellationToken = new CancellationTokenSource().Token;

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = []
        };

        WorkspaceFileService
            .ReadAsync(filePath, cancellationToken)
            .Returns("Content");

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, cancellationToken);

        // Assert
        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                "Content",
                Arg.Any<Encoding>(),
                cancellationToken);
    }
}