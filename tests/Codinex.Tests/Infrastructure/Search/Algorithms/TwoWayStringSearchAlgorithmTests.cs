using Codinex.Core.Interfaces.Search;
using Codinex.Infrastructure.Search.Algorithms;
using Codinex.Tests.Infrastructure.Search.Algorithms.Base;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.Search.Algorithms;

[TestFixture]
public sealed class TwoWayStringSearchAlgorithmTests : ExactAlgorithmContractTestBase
{
    protected override IStringSearchAlgorithm CreateSut() => new TwoWayStringSearchAlgorithm();
}
