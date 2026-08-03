using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_After : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldIgnoreAfter_WhenAfterIsNull()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello",
            After = null
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
    }

    [Test]
    public void Match_ShouldIgnoreAfter_WhenAfterIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello",
            After = string.Empty
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
    }

    [Test]
    public void Match_ShouldReturnSuccess_WhenAfterMatches()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello",
            After = " World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(0);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenAfterDoesNotMatch()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello",
            After = " Everyone"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenAfterIsLongerThanRemainingContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World",
            After = " Very Long Suffix"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
    }

    [Test]
    public void Match_ShouldReduceMultipleMatchesToSingleMatch_WhenAfterMatchesOnlyOne()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            After = ":A"
        };

        // Act
        var result = sut.Match("Value:A Value:B", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(0);
    }

    [Test]
    public void Match_ShouldKeepMultipleMatches_WhenAfterMatchesAll()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            After = ":"
        };

        // Act
        var result = sut.Match("Value:: Value::", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(2);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenAfterRemovesAllMatches()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            After = ":X"
        };

        // Act
        var result = sut.Match("Value:A Value:B", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }
}