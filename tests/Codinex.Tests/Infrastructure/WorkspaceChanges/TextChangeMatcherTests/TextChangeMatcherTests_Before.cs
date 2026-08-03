using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_Before : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldIgnoreBefore_WhenBeforeIsNull()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World",
            Before = null
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
    }

    [Test]
    public void Match_ShouldIgnoreBefore_WhenBeforeIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World",
            Before = string.Empty
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
    }

    [Test]
    public void Match_ShouldReturnSuccess_WhenBeforeMatches()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World",
            Before = "Hello "
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(6);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenBeforeDoesNotMatch()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World",
            Before = "Hi "
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenBeforeIsLongerThanAvailableContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello",
            Before = "VeryLongPrefix"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
    }

    [Test]
    public void Match_ShouldReduceMultipleMatchesToSingleMatch_WhenBeforeMatchesOnlyOne()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "A:"
        };

        // Act
        var result = sut.Match("A:Value B:Value", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(2);
    }

    [Test]
    public void Match_ShouldKeepMultipleMatches_WhenBeforeMatchesAll()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = ":"
        };

        // Act
        var result = sut.Match("A:Value B:Value", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(2);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenBeforeRemovesAllMatches()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "X:"
        };

        // Act
        var result = sut.Match("A:Value B:Value", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }
}