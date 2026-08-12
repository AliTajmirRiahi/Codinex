using System;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;
using Codinex.Infrastructure.Search.Algorithms;
using Codinex.Tests.Infrastructure.Search.Algorithms.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.Search.Algorithms;

[TestFixture]
public sealed class BitapStringSearchAlgorithmTests : ExactAlgorithmContractTestBase
{
    protected override IStringSearchAlgorithm CreateSut() => new BitapStringSearchAlgorithm();

    protected override int MaxSupportedPatternLength => BitapStringSearchAlgorithm.MaxPatternLength;

    [Test]
    public void Search_PatternLongerThanMaxLength_ThrowsNotSupported()
    {
        var sut = CreateSut();
        var pattern = new string('a', BitapStringSearchAlgorithm.MaxPatternLength + 1);

        var act = () => sut.Search("some text", pattern, new StringSearchOptions());

        act.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void Search_PatternAtExactlyMaxLength_Succeeds()
    {
        var sut = CreateSut();
        var pattern = new string('x', BitapStringSearchAlgorithm.MaxPatternLength);
        var text = "PRE_" + pattern + "_POST";

        var result = sut.Search(text, pattern, new StringSearchOptions());

        result.Should().ContainSingle();
        result[0].StartIndex.Should().Be(4);
    }
}
