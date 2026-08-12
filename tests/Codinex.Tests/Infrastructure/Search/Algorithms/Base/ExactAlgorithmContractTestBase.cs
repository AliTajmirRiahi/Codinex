using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;
using Codinex.Infrastructure.Search.Algorithms;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.Search.Algorithms.Base;

/// <summary>
/// Every exact-match algorithm is run through the same scenario matrix and cross-checked against
/// <see cref="NaiveStringSearchAlgorithm"/>, which is treated as the ground truth per the engine's
/// design rules (section 14: "for every algorithm, compare its results with the Naive implementation").
/// </summary>
public abstract class ExactAlgorithmContractTestBase
{
    private readonly NaiveStringSearchAlgorithm _naive = new();

    protected abstract IStringSearchAlgorithm CreateSut();

    /// <summary>Bitap/BNDM are bounded to a single machine word; override to that limit.</summary>
    protected virtual int MaxSupportedPatternLength => int.MaxValue;

    private static StringSearchOptions Options(SearchMode mode = SearchMode.Exact, bool wholeWord = false)
    {
        return new StringSearchOptions { ComparisonMode = mode, WholeWord = wholeWord, MaxResults = int.MaxValue };
    }

    private void AssertMatchesNaive(string text, string pattern, StringSearchOptions options = null)
    {
        options ??= Options();

        var sut = CreateSut();

        var expected = _naive.Search(text, pattern, options);
        var actual = sut.Search(text, pattern, options);

        actual.Select(m => (m.StartIndex, m.EndIndex))
            .Should().BeEquivalentTo(expected.Select(m => (m.StartIndex, m.EndIndex)), opts => opts.WithStrictOrdering());

        foreach (var match in actual)
        {
            match.MatchedText.Should().Be(text.Substring(match.StartIndex, match.EndIndex - match.StartIndex));
            match.Algorithm.Should().Be(sut.Algorithm);
        }
    }

    [Test]
    public void Search_EmptyText_ReturnsNoMatches()
    {
        var sut = CreateSut();

        var result = sut.Search(string.Empty, "abc", Options());

        result.Should().BeEmpty();
    }

    [Test]
    public void Search_EmptyPattern_Throws()
    {
        var sut = CreateSut();

        var act = () => sut.Search("hello", string.Empty, Options());

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Search_NullText_Throws()
    {
        var sut = CreateSut();

        var act = () => sut.Search(null, "abc", Options());

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Search_NullPattern_Throws()
    {
        var sut = CreateSut();

        var act = () => sut.Search("hello", null, Options());

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Search_PatternLongerThanText_ReturnsNoMatches()
    {
        AssertMatchesNaive("ab", "abcdef");
    }

    [Test]
    public void Search_PatternAtBeginning_MatchesNaive()
    {
        AssertMatchesNaive("HelloWorld", "Hello");
    }

    [Test]
    public void Search_PatternAtEnd_MatchesNaive()
    {
        AssertMatchesNaive("HelloWorld", "World");
    }

    [Test]
    public void Search_PatternInMiddle_MatchesNaive()
    {
        AssertMatchesNaive("XXXHelloXXX", "Hello");
    }

    [Test]
    public void Search_NoMatch_MatchesNaive()
    {
        AssertMatchesNaive("The quick brown fox", "zzz");
    }

    [Test]
    public void Search_MultipleMatches_MatchesNaive()
    {
        AssertMatchesNaive("abc abc abc abc", "abc");
    }

    [Test]
    public void Search_OverlappingMatches_MatchesNaive()
    {
        AssertMatchesNaive("aaaaaa", "aaa");
    }

    [Test]
    public void Search_RepeatedCharacterPattern_MatchesNaive()
    {
        AssertMatchesNaive(new string('a', 40), new string('a', 5));
    }

    [Test]
    public void Search_CaseSensitive_MatchesNaive()
    {
        AssertMatchesNaive("Hello hello HELLO", "Hello", Options());
    }

    [Test]
    public void Search_CaseInsensitive_MatchesNaive()
    {
        AssertMatchesNaive("Hello hello HELLO", "hello", Options(SearchMode.CaseInsensitive));
    }

    [Test]
    public void Search_WholeWord_MatchesNaive()
    {
        AssertMatchesNaive("cat category cat scatter cat", "cat", Options(wholeWord: true));
    }

    [Test]
    public void Search_Unicode_MatchesNaive()
    {
        AssertMatchesNaive("héllo wörld — 日本語 テスト 日本語 done", "日本語");
    }

    [Test]
    public void Search_LargeText_MatchesNaive()
    {
        var builder = new StringBuilder();

        for (var i = 0; i < 5000; i++)
            builder.Append("the quick brown fox jumps over the lazy dog. ");

        builder.Append("NEEDLE-MARKER");
        builder.Append("filler text ");
        builder.Append("NEEDLE-MARKER");

        AssertMatchesNaive(builder.ToString(), "NEEDLE-MARKER");
    }

    [Test]
    public void Search_LargePattern_MatchesNaive()
    {
        var patternLength = Math.Min(MaxSupportedPatternLength, 64);
        var pattern = string.Concat(Enumerable.Range(0, patternLength).Select(i => (char)('a' + i % 26)));

        var text = "prefix-noise-" + pattern + "-suffix-noise-" + pattern + "-end";

        AssertMatchesNaive(text, pattern);
    }
}
