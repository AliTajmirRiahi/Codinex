using System;
using Codinex.Core.Models.Search;
using Codinex.Infrastructure.Search.Algorithms;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.Search.Algorithms;

[TestFixture]
public sealed class FuzzyStringSearchAlgorithmTests
{
    private static FuzzyStringSearchAlgorithm CreateSut() => new();

    [Test]
    public void Search_ExactOccurrence_ScoresOne()
    {
        var sut = CreateSut();

        var result = sut.Search("the quick brown fox", "quick", new StringSearchOptions { MaxEditDistance = 2 });

        result.Should().ContainSingle(m => m.StartIndex == 4 && m.Score == 1.0);
    }

    [Test]
    public void Search_OneSubstitution_IsFoundWithinEditDistance()
    {
        var sut = CreateSut();

        // "quack" is one substitution away from "quick".
        var result = sut.Search("the quack brown fox", "quick", new StringSearchOptions { MaxEditDistance = 1 });

        result.Should().ContainSingle();
        result[0].MatchedText.Should().Be("quack");
        result[0].Score.Should().BeLessThan(1.0);
        result[0].MatchType.Should().Be(MatchType.Fuzzy);
    }

    [Test]
    public void Search_OneInsertion_IsFoundWithinEditDistance()
    {
        var sut = CreateSut();

        // "quicck" has one extra character compared to "quick".
        var result = sut.Search("the quicck brown fox", "quick", new StringSearchOptions { MaxEditDistance = 1 });

        result.Should().Contain(m => m.MatchedText == "quicck");
    }

    [Test]
    public void Search_DistanceExceedsMax_ReturnsNoMatches()
    {
        var sut = CreateSut();

        var result = sut.Search("completely unrelated text", "quick", new StringSearchOptions { MaxEditDistance = 1 });

        result.Should().BeEmpty();
    }

    [Test]
    public void Search_MinimumScoreFiltersLowQualityMatches()
    {
        var sut = CreateSut();

        var result = sut.Search(
            "the quacky brown fox",
            "quick",
            new StringSearchOptions { MaxEditDistance = 3, MinimumScore = 0.9 });

        result.Should().BeEmpty();
    }

    [Test]
    public void Search_CaseInsensitive_MatchesAcrossCase()
    {
        var sut = CreateSut();

        var result = sut.Search(
            "THE QUICK BROWN FOX",
            "quick",
            new StringSearchOptions { MaxEditDistance = 0, ComparisonMode = SearchMode.CaseInsensitive });

        result.Should().ContainSingle(m => m.MatchedText == "QUICK");
    }

    [Test]
    public void Search_RespectsMaxResults()
    {
        var sut = CreateSut();

        var result = sut.Search(
            "cat cot cut cat cot cut",
            "cat",
            new StringSearchOptions { MaxEditDistance = 1, MaxResults = 2 });

        result.Should().HaveCount(2);
    }

    [Test]
    public void Search_EmptyPattern_Throws()
    {
        var sut = CreateSut();

        var act = () => sut.Search("hello", string.Empty, new StringSearchOptions());

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Search_NullText_Throws()
    {
        var sut = CreateSut();

        var act = () => sut.Search(null, "abc", new StringSearchOptions());

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Search_EmptyText_ReturnsNoMatches()
    {
        var sut = CreateSut();

        var result = sut.Search(string.Empty, "abc", new StringSearchOptions());

        result.Should().BeEmpty();
    }
}
