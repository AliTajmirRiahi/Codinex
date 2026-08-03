using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_BeforeAndAfter : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldReturnSuccess_WhenBeforeAndAfterMatch()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "A:",
            After = ":B"
        };

        // Act
        var result = sut.Match("A:Value:B", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.MatchCount.Should().Be(1);
        result.StartIndex.Should().Be(2);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenBeforeMatchesAndAfterDoesNotMatch()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "A:",
            After = ":X"
        };

        // Act
        var result = sut.Match("A:Value:B", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenBeforeDoesNotMatchAndAfterMatches()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "X:",
            After = ":B"
        };

        // Act
        var result = sut.Match("A:Value:B", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenBeforeAndAfterDoNotMatch()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "X:",
            After = ":Y"
        };

        // Act
        var result = sut.Match("A:Value:B", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }

    [Test]
    public void Match_ShouldReduceMultipleMatchesToSingleMatch_WhenBeforeAndAfterIdentifyOneMatch()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "B:",
            After = ":2"
        };

        // Act
        var result = sut.Match("A:Value:1 B:Value:2", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.MatchCount.Should().Be(1);
        result.MatchedText.Should().Be("Value");
    }

    [Test]
    public void Match_ShouldReturnMultipleMatches_WhenBeforeAndAfterStillMatchMultipleLocations()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = ":",
            After = ":"
        };

        // Act
        var result = sut.Match("A:Value:B C:Value:D", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(2);
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenBeforeFiltersOneMatchAndAfterFiltersTheOther()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Value",
            Before = "A:",
            After = ":2"
        };

        // Act
        var result = sut.Match("A:Value:1 B:Value:2", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
    }
}