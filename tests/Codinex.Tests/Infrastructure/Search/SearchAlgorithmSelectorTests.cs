using System;
using System.Collections.Generic;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;
using Codinex.Infrastructure.Search;
using Codinex.Infrastructure.Search.Algorithms;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.Search;

[TestFixture]
public sealed class SearchAlgorithmSelectorTests
{
    private static IReadOnlyList<IStringSearchAlgorithm> AllAlgorithms() => new List<IStringSearchAlgorithm>
    {
        new NaiveStringSearchAlgorithm(),
        new KmpStringSearchAlgorithm(),
        new BoyerMooreStringSearchAlgorithm(),
        new BoyerMooreHorspoolStringSearchAlgorithm(),
        new RabinKarpStringSearchAlgorithm(),
        new TwoWayStringSearchAlgorithm(),
        new BitapStringSearchAlgorithm(),
        new BndmStringSearchAlgorithm(),
        new AhoCorasickStringSearchAlgorithm(),
        new FuzzyStringSearchAlgorithm()
    };

    private static SearchAlgorithmSelector CreateSut() => new();

    [Test]
    public void Select_SmallPattern_ReturnsNaive()
    {
        var sut = CreateSut();

        var request = new SearchRequest { Text = "some text", Pattern = "ab", Options = new StringSearchOptions() };

        var result = sut.Select(request, AllAlgorithms());

        result.Algorithm.Should().Be(SearchAlgorithmType.Naive);
    }

    [Test]
    public void Select_NormalPattern_ReturnsBoyerMooreHorspool()
    {
        var sut = CreateSut();

        var request = new SearchRequest { Text = "some text", Pattern = "a normal pattern", Options = new StringSearchOptions() };

        var result = sut.Select(request, AllAlgorithms());

        result.Algorithm.Should().Be(SearchAlgorithmType.BoyerMooreHorspool);
    }

    [Test]
    public void Select_LargeText_ReturnsKmp()
    {
        var sut = CreateSut();

        var request = new SearchRequest
        {
            Text = new string('x', SearchAlgorithmSelector.LargeTextLength + 1),
            Pattern = "a normal pattern",
            Options = new StringSearchOptions()
        };

        var result = sut.Select(request, AllAlgorithms());

        result.Algorithm.Should().Be(SearchAlgorithmType.Kmp);
    }

    [Test]
    public void Select_MultiPatternRequest_ReturnsAhoCorasick()
    {
        var sut = CreateSut();

        var request = new SearchRequest
        {
            Text = "some text",
            Patterns = new[] { "a", "b" },
            Options = new StringSearchOptions()
        };

        var result = sut.Select(request, AllAlgorithms());

        result.Algorithm.Should().Be(SearchAlgorithmType.AhoCorasick);
    }

    [Test]
    public void Select_FuzzyRequest_ReturnsFuzzy()
    {
        var sut = CreateSut();

        var request = new SearchRequest
        {
            Text = "some text",
            Pattern = "sme txt",
            Options = new StringSearchOptions { EnableFuzzy = true }
        };

        var result = sut.Select(request, AllAlgorithms());

        result.Algorithm.Should().Be(SearchAlgorithmType.Fuzzy);
    }

    [Test]
    public void Select_MultiPatternRequested_ButNoMultiPatternAlgorithmRegistered_Throws()
    {
        var sut = CreateSut();

        var request = new SearchRequest
        {
            Text = "some text",
            Patterns = new[] { "a", "b" },
            Options = new StringSearchOptions()
        };

        var act = () => sut.Select(request, new List<IStringSearchAlgorithm> { new NaiveStringSearchAlgorithm() });

        act.Should().Throw<InvalidOperationException>();
    }
}
