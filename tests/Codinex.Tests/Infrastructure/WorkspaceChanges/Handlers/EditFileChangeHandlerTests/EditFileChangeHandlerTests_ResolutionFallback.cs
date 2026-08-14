using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests;

[TestFixture]
public sealed class EditFileChangeHandlerTests_ResolutionFallback
    : EditFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldFallBackToResolutionResult_WhenSearchFailsAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "stale search text",
            Replace = "Codinex"
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
                Status = TextChangeMatchStatus.SearchNotFound,
                Error = "Search text not found."
            });

        ResolutionContext.Current = ChangeValidationResult.Successful(
        [
            new ResolvedFileChange
            {
                FilePath = filePath,
                TextChanges =
                [
                    new ResolvedTextChange
                    {
                        Id = textChange.Id,
                        Order = 1,
                        Search = "World",
                        Replace = "Codinex",
                        Range = new TextRange { Start = 6, Length = 5 },
                        OriginalText = "World",
                        ResultText = "Codinex"
                    }
                ]
            }
        ]);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        await WorkspaceFileService.Received(1).WriteAsync(
            filePath,
            "Hello Codinex",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldFail_WhenSearchFailsAndNoResolutionIsAvailableAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "stale search text",
            Replace = "Codinex"
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
        var result = await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
    }

    [Test]
    public async Task HandleAsync_ShouldFail_WhenSearchFailsAndResolutionHasNoMatchingFileAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "stale search text",
            Replace = "Codinex"
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
                Status = TextChangeMatchStatus.SearchNotFound,
                Error = "Search text not found."
            });

        ResolutionContext.Current = ChangeValidationResult.Successful(
        [
            new ResolvedFileChange
            {
                FilePath = @"C:\Test\SomeOtherFile.cs",
                TextChanges = []
            }
        ]);

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
        var result = await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
    }

    [Test]
    public async Task HandleAsync_ShouldPreferLiveSearchMatch_OverResolutionResultAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "World",
            Replace = "Codinex"
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

        // A resolution is available, but should never be consulted since the live Search succeeds.
        ResolutionContext.Current = ChangeValidationResult.Successful(
        [
            new ResolvedFileChange
            {
                FilePath = filePath,
                TextChanges =
                [
                    new ResolvedTextChange
                    {
                        Id = textChange.Id,
                        Order = 1,
                        Range = new TextRange { Start = 0, Length = 0 },
                        OriginalText = string.Empty,
                        ResultText = "SHOULD NOT BE USED"
                    }
                ]
            }
        ]);

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1).WriteAsync(
            filePath,
            "Hello Codinex",
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
