using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Resolution.EditFileChangeResolverTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Resolution.EditFileChangeResolverTests;

[TestFixture]
public sealed class EditFileChangeResolverTests_Target : EditFileChangeResolverBaseTests
{
    [Test]
    public async Task ResolveAsync_ShouldFallBackToTarget_WhenSearchIsNotFoundAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.html";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "this text does not exist in the file",
            Target = "about-peek-container",
            Operation = TextChangeOperations.InsertAfter,
            Content = "<div>New</div>"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("<div id=\"about-peek-container\"></div>");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var resolvedChange = result.Changes[0].TextChanges[0];
        resolvedChange.Search.Should().Be("about-peek-container");
        resolvedChange.ResultText.Should().Be("<div>New</div>");

        textChange.Search.Should().Be("about-peek-container");
    }

    [Test]
    public async Task ResolveAsync_ShouldFallBackToTarget_WhenSearchMatchesMultipleTimesAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.html";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "div",
            Target = "unique-marker",
            Operation = TextChangeOperations.InsertBefore,
            Content = "<span>New</span>"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("<div></div><div id=\"unique-marker\"></div>");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Changes[0].TextChanges[0].Search.Should().Be("unique-marker");
    }

    [Test]
    public async Task ResolveAsync_ShouldPreferSearch_WhenSearchAlreadyMatchesUniquelyAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.html";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "World",
            Target = "some-other-anchor",
            Content = "Codinex"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World some-other-anchor");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Changes[0].TextChanges[0].Search.Should().Be("World");
    }
}
