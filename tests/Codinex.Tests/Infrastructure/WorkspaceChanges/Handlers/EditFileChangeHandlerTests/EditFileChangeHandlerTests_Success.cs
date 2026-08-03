using Codinex.Infrastructure.WorkspaceChanges;
using Codinex.VisualStudio.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests.Base;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests;

[TestFixture]
public sealed class EditFileChangeHandlerTests_Success
    : EditFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldReturnSuccessfulResult_WhenSingleTextChangeIsAppliedAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";
        const string originalContent = "Hello World";
        const string updatedContent = "Hello Codify";

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges =
            [
                new TextFileChange
                {
                    Id = Guid.NewGuid(),
                    Order = 1,
                    Search = "World",
                    Replace = "Codify"
                }
            ]
        };

        TextChangeMatcher
            .Match(originalContent, change.TextChanges[0])
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 6,
                Length = 5
            });

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns(originalContent);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();

        await WorkspaceFileService.Received(1).WriteAsync(
            filePath,
            updatedContent,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldApplyMultipleTextChanges_InOrderAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var change1 = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 2,
            Search = "World",
            Replace = "Codify"
        };

        var change2 = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "Hello",
            Replace = "Hi"
        };

        var change = new EditFileChange
        {
            FilePath = filePath,
            TextChanges = [change1, change2]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        TextChangeMatcher
            .Match("Hello World", change2)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 0,
                Length = 5
            });

        TextChangeMatcher
            .Match("Hi World", change1)
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
            TextChangeMatcher.Match("Hello World", change2);
            TextChangeMatcher.Match("Hi World", change1);
        });

        await WorkspaceFileService.Received(1).WriteAsync(
            filePath,
            "Hi Codify",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldReadFileOnlyOnceAsync()
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
            .Returns("A");

        TextChangeMatcher
            .Match("A", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 0,
                Length = 1
            });

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1)
            .ReadAsync(filePath, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldWriteFileOnlyOnce_WhenAllChangesSucceedAsync()
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
            .Returns("A");

        TextChangeMatcher
            .Match("A", textChange)
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                StartIndex = 0,
                Length = 1
            });

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(change, CancellationToken.None);

        // Assert
        await WorkspaceFileService.Received(1).WriteAsync(
            filePath,
            "B",
            cancellationToken: Arg.Any<CancellationToken>());
    }
}