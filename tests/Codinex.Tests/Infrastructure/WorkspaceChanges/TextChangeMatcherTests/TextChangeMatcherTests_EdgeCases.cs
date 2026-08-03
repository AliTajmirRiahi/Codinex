using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_EdgeCases : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldHandleAdjacentMatches()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "abc"
        };

        // Act
        var result = sut.Match("abcabc", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(2);
    }

    [Test]
    public void Match_ShouldHandleMultiLineSearch()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Line2\r\nLine3"
        };

        // Act
        var result = sut.Match("Line1\r\nLine2\r\nLine3\r\nLine4", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.MatchedText.Should().Be("Line2\r\nLine3");
    }

    [Test]
    public void Match_ShouldBeCaseSensitive()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "world"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
    }

    [Test]
    public void Match_ShouldBeWhitespaceSensitive()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello  World"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
    }

    [Test]
    public void Match_ShouldBeTabSensitive()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Hello\tWorld"
        };

        // Act
        var result = sut.Match("Hello World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
    }

    [Test]
    public void Match_ShouldDifferentiateBetweenCrLfAndLf()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "A\r\nB"
        };

        // Act
        var result = sut.Match("A\nB", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
    }

    [Test]
    public void Match_ShouldSupportUnicodeCharacters()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "こんにちは"
        };

        // Act
        var result = sut.Match("ABC こんにちは XYZ", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
    }

    [Test]
    public void Match_ShouldSupportPersianCharacters()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "سلام"
        };

        // Act
        var result = sut.Match("123 سلام 456", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
    }

    [Test]
    public void Match_ShouldSupportEmoji()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "🚀"
        };

        // Act
        var result = sut.Match("Hello 🚀 World", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.MatchedText.Should().Be("🚀");
    }

    [Test]
    public void Match_ShouldHandleSearchAtEndOfFile()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "End"
        };

        // Act
        var result = sut.Match("Beginning Middle End", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be("Beginning Middle ".Length);
    }
}