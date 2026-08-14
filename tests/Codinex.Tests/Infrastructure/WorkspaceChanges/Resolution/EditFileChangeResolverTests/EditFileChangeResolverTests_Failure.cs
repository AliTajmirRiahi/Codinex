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
public sealed class EditFileChangeResolverTests_Failure : EditFileChangeResolverBaseTests
{
    [Test]
    public async Task ResolveAsync_ShouldFail_WhenSearchNotFoundAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "does not exist",
            Replace = "Codinex"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("Hello World");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Changes.Should().BeEmpty();
        result.Errors.Should().ContainSingle();
        result.Errors[0].ChangeId.Should().Be(textChange.Id);
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.SearchNotFound);
    }

    [Test]
    public async Task ResolveAsync_ShouldPickEarliestOccurrence_WhenSearchMatchesMultipleTimes_AndBeforeAfterDontDisambiguateAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "abc",
            Replace = "xyz"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("abc abc");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert: still ambiguous by position, but resolution now picks the earliest occurrence
        // instead of failing the whole changeset.
        result.Success.Should().BeTrue();

        var resolvedChange = result.Changes[0].TextChanges[0];
        resolvedChange.Range.Start.Should().Be(0);
    }

    [Test]
    public async Task ResolveAsync_ShouldFail_WhenBeforeDoesNotMatchAnyCandidateAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Before = "never present ",
            Search = "abc",
            Replace = "xyz"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns("abc");

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.SearchNotFound);
    }

    [Test]
    public async Task ResolveAsync_ShouldFail_WhenFileCannotBeReadAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\Missing.cs";

        var textChange = new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "A",
            Replace = "B"
        };

        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            TextChanges = [textChange]
        };

        WorkspaceFileService
            .ReadAsync(filePath, Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("File not found."));

        var changeSet = new WorkspaceChangeSet();
        changeSet.Changes.Add(change);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveAsync(changeSet, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors[0].ChangeId.Should().Be(change.Id);
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.FileNotFound);
    }

    [Test]
    public void ResolveAsync_ShouldThrow_WhenChangeSetIsNullAsync()
    {
        var sut = CreateSut();

        Func<Task> act = () => sut.ResolveAsync(null!, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentNullException>();
    }
}
