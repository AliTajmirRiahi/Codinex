using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests.Base;
using Codify.VisualStudio.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests;

[TestFixture]
public sealed class EditFileChangeHandlerTests_Failure
    : EditFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldReturnFailure_WhenSearchTextIsNotFoundAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "World",
            Replace = "Codify"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [textChange]
        };

        var error = new WorkspaceChangeError();

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello");

        TextChangeMatcher
            .Match("Hello", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.SearchNotFound,
                Error = "Search text not found."
            });

        WorkspaceChangeErrorFactory
            .Create(
                WorkspaceChangeErrorCode.SearchNotFound,
                filePath,
                textChange.Id,
                "Search text not found.")
            .Returns(error);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeSameAs(error);

        await WorkspaceFileService.DidNotReceive()
            .WriteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldReturnFailure_WhenMultipleMatchesAreFoundAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "World",
            Replace = "Codify"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [textChange]
        };

        var error = new WorkspaceChangeError();

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World World");

        TextChangeMatcher
            .Match("Hello World World", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.MultipleMatches,
                Error = "Multiple matches found."
            });

        WorkspaceChangeErrorFactory
            .Create(
                WorkspaceChangeErrorCode.MultipleMatches,
                filePath,
                textChange.Id,
                "Multiple matches found.")
            .Returns(error);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeSameAs(error);

        await WorkspaceFileService.DidNotReceive()
            .WriteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldReturnFailure_WhenUniqueMatchCannotBeDeterminedAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "World",
            Replace = "Codify"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [textChange]
        };

        var error = new WorkspaceChangeError();

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        TextChangeMatcher
            .Match("Hello World", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.NoUniqueMatch,
                Error = "Unable to determine a unique match."
            });

        WorkspaceChangeErrorFactory
            .Create(
                WorkspaceChangeErrorCode.NoUniqueMatch,
                filePath,
                textChange.Id,
                "Unable to determine a unique match.")
            .Returns(error);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeSameAs(error);

        await WorkspaceFileService.DidNotReceive()
            .WriteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldStopProcessingRemainingChanges_WhenMatchFailsAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var first = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "A",
            Replace = "B"
        };

        var second = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 2,
            Search = "C",
            Replace = "D"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [first, second]
        };

        var error = new WorkspaceChangeError();

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("ABC");

        TextChangeMatcher
            .Match("ABC", first)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.SearchNotFound,
                Error = "Search text not found."
            });

        WorkspaceChangeErrorFactory
            .Create(
                WorkspaceChangeErrorCode.SearchNotFound,
                filePath,
                first.Id,
                "Search text not found.")
            .Returns(error);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        TextChangeMatcher.Received(1).Match("ABC", first);
        TextChangeMatcher.DidNotReceive().Match(Arg.Any<string>(), second);

        await WorkspaceFileService.DidNotReceive()
            .WriteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldCreateWorkspaceChangeError_FromMatcherFailureAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "A",
            Replace = "B"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("ABC");

        TextChangeMatcher
            .Match("ABC", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.SearchNotFound,
                Error = "Search text not found."
            });

        var error = new WorkspaceChangeError();

        WorkspaceChangeErrorFactory
            .Create(
                WorkspaceChangeErrorCode.SearchNotFound,
                filePath,
                textChange.Id,
                "Search text not found.")
            .Returns(error);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        WorkspaceChangeErrorFactory.Received(1).Create(
            WorkspaceChangeErrorCode.SearchNotFound,
            filePath,
            textChange.Id,
            "Search text not found.");
    }
}