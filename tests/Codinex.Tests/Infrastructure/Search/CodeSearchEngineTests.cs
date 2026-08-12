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
public sealed class CodeSearchEngineTests
{
    private static CodeSearchEngine CreateSut()
    {
        var algorithms = new List<IStringSearchAlgorithm>
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

        return new CodeSearchEngine(algorithms, new SearchAlgorithmSelector());
    }

    [Test]
    public void Search_ComputesLineAndColumn_ForMultiLineText()
    {
        var sut = CreateSut();

        var text = "line one\nline two\nline three with TARGET here\n";

        var result = sut.Search(new SearchRequest
        {
            Text = text,
            Pattern = "TARGET",
            Options = new StringSearchOptions { Algorithm = SearchAlgorithmType.BoyerMooreHorspool }
        });

        result.Matches.Should().ContainSingle();

        var match = result.Matches[0];
        match.StartLine.Should().Be(3);
        match.StartColumn.Should().Be(17);
        match.EndLine.Should().Be(3);
        match.EndColumn.Should().Be(23);
    }

    [Test]
    public void Search_MultipleMatches_ReturnsAllCandidates_CallerDisambiguates()
    {
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = "<button>About</button><button>About</button>",
            Pattern = "<button>About</button>",
            Options = new StringSearchOptions()
        });

        result.Matches.Should().HaveCount(2);
    }

    [Test]
    public void Search_AutoWithSmallPattern_SelectsNaive()
    {
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = "abc def abc",
            Pattern = "ab",
            Options = new StringSearchOptions()
        });

        result.AlgorithmUsed.Should().Be(SearchAlgorithmType.Naive);
    }

    [Test]
    public void Search_AutoWithNormalPattern_SelectsBoyerMooreHorspool()
    {
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = "some source code containing a target phrase",
            Pattern = "target phrase",
            Options = new StringSearchOptions()
        });

        result.AlgorithmUsed.Should().Be(SearchAlgorithmType.BoyerMooreHorspool);
    }

    [Test]
    public void Search_MultiPattern_UsesAhoCorasick()
    {
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = "About Settings Help",
            Patterns = new[] { "About", "Settings", "Help" },
            Options = new StringSearchOptions()
        });

        result.AlgorithmUsed.Should().Be(SearchAlgorithmType.AhoCorasick);
        result.Matches.Should().HaveCount(3);
    }

    [Test]
    public void Search_ExplicitAlgorithmNotRegistered_Throws()
    {
        var sut = new CodeSearchEngine(
            new List<IStringSearchAlgorithm> { new NaiveStringSearchAlgorithm() },
            new SearchAlgorithmSelector());

        var act = () => sut.Search(new SearchRequest
        {
            Text = "abc",
            Pattern = "a",
            Options = new StringSearchOptions { Algorithm = SearchAlgorithmType.Kmp }
        });

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Search_MinimumScoreFiltersResults()
    {
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = "the quacky brown fox",
            Pattern = "quick",
            Options = new StringSearchOptions
            {
                Algorithm = SearchAlgorithmType.Fuzzy,
                MaxEditDistance = 3,
                MinimumScore = 0.95
            }
        });

        result.Matches.Should().BeEmpty();
    }

    [Test]
    public void Search_NullRequest_Throws()
    {
        var sut = CreateSut();

        var act = () => sut.Search(null);

        act.Should().Throw<ArgumentNullException>();
    }
}
