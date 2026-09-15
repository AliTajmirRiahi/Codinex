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
public sealed class EditFileChangeResolverTests_Search : EditFileChangeResolverBaseTests
{
    [Test]
    public async Task ResolveAsync_ShouldPlanReplace_WhenSearchMatchesExactlyOnceAsync()
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
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        // Locating the text is the matcher's job and is covered by TextChangeMatcher's own tests.
        // Here it is stubbed so the assertions exercise the resolver's planning: the range it
        // derives, the original text it slices out, and the line/column it computes.
        TextChangeMatcher
            .Match(Arg.Any<string>(), Arg.Any<TextFileChange>())
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                MatchCount = 1,
                StartIndex = 6,
                Length = 5,
                MatchedText = "World"
            });

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var resolvedFile = result.Changes.Should().ContainSingle().Subject;
        resolvedFile.FilePath.Should().Be(filePath);

        var resolvedChange = resolvedFile.TextChanges.Should().ContainSingle().Subject;
        resolvedChange.Search.Should().Be("World");
        resolvedChange.OriginalText.Should().Be("World");
        resolvedChange.ResultText.Should().Be("Codinex");

        resolvedChange.Range.Start.Should().Be(6);
        resolvedChange.Range.Length.Should().Be(5);
        resolvedChange.Range.StartLine.Should().Be(1);
        resolvedChange.Range.StartColumn.Should().Be(7);

        textChange.Search.Should().Be("World");
    }

    [Test]
    public async Task ResolveAsync_ShouldPlanReplace_WithEmptyReplaceText_ActsAsADeleteAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "World"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        TextChangeMatcher
            .Match(Arg.Any<string>(), Arg.Any<TextFileChange>())
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                MatchCount = 1,
                StartIndex = 6,
                Length = 5,
                MatchedText = "World"
            });

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var resolvedChange = result.Changes[0].TextChanges[0];
        resolvedChange.OriginalText.Should().Be("World");
        resolvedChange.ResultText.Should().BeEmpty();
    }

    [Test]
    public async Task ResolveAsync_ShouldDisambiguate_UsingBeforeAndAfterAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Before = "one ",
            Search = "cat",
            After = " here",
            Replace = "dog"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        // The matcher resolves Before/After itself; its own fixtures cover that. The resolver's
        // contract here is that it plans against whichever occurrence the matcher selected.
        TextChangeMatcher
            .Match(Arg.Any<string>(), Arg.Any<TextFileChange>())
            .Returns(new TextChangeMatchResult
            {
                Status = TextChangeMatchStatus.Success,
                MatchCount = 1,
                StartIndex = "cat elsewhere, one ".Length,
                Length = 3,
                MatchedText = "cat"
            });

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("cat elsewhere, one cat here, cat too");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var resolvedChange = result.Changes[0].TextChanges[0];
        resolvedChange.Range.Start.Should().Be("cat elsewhere, one ".Length);
    }


    [Test]
    public async Task ResolveAsync_ShouldSkipNonEditFileChangesAsync()
    {
        // Arrange
        var change = new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "New.cs",
            Content = "content"
        };

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Changes.Should().BeEmpty();

        await WorkspaceFileService.DidNotReceive()
            .ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
