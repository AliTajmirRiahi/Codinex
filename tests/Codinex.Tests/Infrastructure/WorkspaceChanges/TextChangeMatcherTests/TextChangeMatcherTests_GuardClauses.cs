using System;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_GuardClauses : TextChangeMatcherTestBase
{
    [Test]
    public void Match_ShouldThrowArgumentNullException_WhenContentIsNull()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "Test"
        };

        // Act
        Action act = () => sut.Match(null, change);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("content");
    }

    [Test]
    public void Match_ShouldThrowArgumentNullException_WhenChangeIsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Action act = () => sut.Match("Content", null);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("change");
    }

    // Search is model-supplied, so an empty value is reported as a result the agent can correct
    // rather than thrown: no caller of Match catches exceptions, and throwing here would turn a
    // recoverable model mistake into a crash.

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenSearchIsNull()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = null
        };

        // Act
        var result = sut.Match("Content", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
        result.Error.Should().Be("Unable to search with empty text.");
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenSearchIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = string.Empty
        };

        // Act
        var result = sut.Match("Content", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
        result.Error.Should().Be("Unable to search with empty text.");
    }

    [Test]
    public void Match_ShouldReturnNoUniqueMatch_WhenSearchIsWhiteSpace()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "   "
        };

        // Act
        var result = sut.Match("Content", change);

        // Assert
        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
        result.MatchCount.Should().Be(0);
        result.Error.Should().Be("Unable to search with empty text.");
    }
}
