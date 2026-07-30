using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_Search : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldReturnSearchNotFound_WhenSearchDoesNotExist()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "XYZ"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
        result.MatchCount.Should().Be(0);
        result.Error.Should().Be("Search text was not found.");
    }

    [Test]
    public void Match_ShouldReturnSuccess_WhenSearchExistsOnce()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.MatchCount.Should().Be(1);
        result.StartIndex.Should().Be(6);
        result.Length.Should().Be(5);
        result.MatchedText.Should().Be("World");
        result.Error.Should().BeNull();
    }

    [Test]
    public void Match_ShouldReturnMultipleMatches_WhenSearchExistsTwice()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "abc"
        };

        // Act
        var result = sut.Match("abc 123 abc", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(2);
        result.Error.Should().Be("Multiple matching locations were found.");
    }

    [Test]
    public void Match_ShouldReturnMultipleMatches_WhenSearchExistsThreeTimes()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "abc"
        };

        // Act
        var result = sut.Match("abc abc abc", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(3);
    }

    [Test]
    public void Match_ShouldFindSearchAtBeginningOfContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(0);
    }

    [Test]
    public void Match_ShouldFindSearchAtEndOfContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(6);
    }

    [Test]
    public void Match_ShouldMatchWholeContent()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(0);
        result.Length.Should().Be(11);
    }

    [Test]
    public void Match_ShouldReturnSearchNotFound_WhenContentIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello"
        };

        // Act
        var result = sut.Match(string.Empty, change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
        result.MatchCount.Should().Be(0);
    }
}