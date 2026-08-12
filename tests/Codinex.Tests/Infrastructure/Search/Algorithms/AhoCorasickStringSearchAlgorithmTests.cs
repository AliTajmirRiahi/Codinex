using System;
using System.Linq;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;
using Codinex.Infrastructure.Search.Algorithms;
using Codinex.Tests.Infrastructure.Search.Algorithms.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.Search.Algorithms;

[TestFixture]
public sealed class AhoCorasickStringSearchAlgorithmTests : ExactAlgorithmContractTestBase
{
    protected override IStringSearchAlgorithm CreateSut() => new AhoCorasickStringSearchAlgorithm();

    [Test]
    public void SearchMultiple_FindsAllPatterns_InOnePass()
    {
        var sut = new AhoCorasickStringSearchAlgorithm();

        var text = "Go to About, then Settings, then Help, then Report Bugs.";
        var patterns = new[] { "About", "Settings", "Help", "Report Bugs" };

        var result = sut.SearchMultiple(text, patterns, new StringSearchOptions());

        result.Should().HaveCount(4);
        result.Should().BeInAscendingOrder(m => m.StartIndex);
        result.Select(m => m.MatchedPattern).Should().BeEquivalentTo(patterns, opts => opts.WithoutStrictOrdering());
    }

    [Test]
    public void SearchMultiple_FindsOverlappingPatterns()
    {
        var sut = new AhoCorasickStringSearchAlgorithm();

        // "she", "he", "hers" all occur (overlapping) inside "ushers".
        var result = sut.SearchMultiple("ushers", new[] { "he", "she", "hers", "his" }, new StringSearchOptions());

        result.Select(m => m.MatchedText).Should().BeEquivalentTo(new[] { "she", "he", "hers" });
    }

    [Test]
    public void SearchMultiple_NullPatterns_Throws()
    {
        var sut = new AhoCorasickStringSearchAlgorithm();

        var act = () => sut.SearchMultiple("text", null, new StringSearchOptions());

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void SearchMultiple_EmptyPatternList_Throws()
    {
        var sut = new AhoCorasickStringSearchAlgorithm();

        var act = () => sut.SearchMultiple("text", Array.Empty<string>(), new StringSearchOptions());

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void SearchMultiple_RespectsMaxResults()
    {
        var sut = new AhoCorasickStringSearchAlgorithm();

        var result = sut.SearchMultiple(
            "aaaaaaaaaa",
            new[] { "a" },
            new StringSearchOptions { MaxResults = 3 });

        result.Should().HaveCount(3);
    }
}
