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

    [Test]
    public void Match_ShouldThrowArgumentException_WhenSearchIsNull()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = null
        };

        // Act
        Action act = () => sut.Match("Content", change);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("change");
    }

    [Test]
    public void Match_ShouldThrowArgumentException_WhenSearchIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = string.Empty
        };

        // Act
        Action act = () => sut.Match("Content", change);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("change");
    }

    [Test]
    public void Match_ShouldThrowArgumentException_WhenSearchIsWhiteSpace()
    {
        // Arrange
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "   "
        };

        // Act
        Action act = () => sut.Match("Content", change);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("change");
    }
}
