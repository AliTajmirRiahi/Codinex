using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_Result : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldReturnCorrectStartIndex()
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
        result.StartIndex.Should().Be(6);
    }

    [Test]
    public void Match_ShouldReturnCorrectLength()
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
        result.Length.Should().Be(5);
    }

    [Test]
    public void Match_ShouldReturnMatchedText()
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
        result.MatchedText.Should().Be("World");
    }

    [Test]
    public void Match_ShouldReturnMatchCountOfOne_WhenSingleMatchExists()
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
        result.MatchCount.Should().Be(1);
    }

    [Test]
    public void Match_ShouldReturnSuccessStatus_WhenSingleMatchExists()
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
    }

    [Test]
    public void Match_ShouldReturnNullError_WhenMatchSucceeds()
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
        result.Error.Should().BeNull();
    }

    [Test]
    public void Match_ShouldReturnMatchedTextEqualToSearch()
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
        result.MatchedText.Should().Be(change.Search);
    }

    [Test]
    public void Match_ShouldReturnLengthEqualToMatchedTextLength()
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
        result.Length.Should().Be(result.MatchedText.Length);
    }
}