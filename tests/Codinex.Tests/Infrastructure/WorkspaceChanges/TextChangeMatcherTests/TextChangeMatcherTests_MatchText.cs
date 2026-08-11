using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.TextChangeMatcherTests;

[TestFixture]
public sealed class TextChangeMatcherTests_MatchText : TextChangeMatcherTestBase
{
    [Test]
    public void MatchText_ShouldReturnSuccess_WhenTextMatchesExactlyOnce()
    {
        var sut = CreateSut();

        var result = sut.MatchText("Hello World", "World");

        result.Status.Should().Be(TextChangeMatchStatus.Success);
        result.StartIndex.Should().Be(6);
        result.Length.Should().Be(5);
    }

    [Test]
    public void MatchText_ShouldReturnSearchNotFound_WhenTextDoesNotExist()
    {
        var sut = CreateSut();

        var result = sut.MatchText("Hello World", "XYZ");

        result.Status.Should().Be(TextChangeMatchStatus.SearchNotFound);
    }

    [Test]
    public void MatchText_ShouldReturnMultipleMatches_WhenTextExistsMoreThanOnce()
    {
        var sut = CreateSut();

        var result = sut.MatchText("abc abc", "abc");

        result.Status.Should().Be(TextChangeMatchStatus.MultipleMatches);
        result.MatchCount.Should().Be(2);
    }

    [Test]
    public void MatchText_ShouldReturnNoUniqueMatch_WhenTextIsEmpty()
    {
        var sut = CreateSut();

        var result = sut.MatchText("Hello World", string.Empty);

        result.Status.Should().Be(TextChangeMatchStatus.NoUniqueMatch);
    }

    [Test]
    public void Match_ShouldDelegateToMatchText_UsingChangeSearch()
    {
        var sut = CreateSut();

        var change = new TextFileChange
        {
            Search = "World"
        };

        var viaMatch = sut.Match("Hello World", change);
        var viaMatchText = sut.MatchText("Hello World", "World");

        viaMatch.Status.Should().Be(viaMatchText.Status);
        viaMatch.StartIndex.Should().Be(viaMatchText.StartIndex);
        viaMatch.Length.Should().Be(viaMatchText.Length);
    }
}
