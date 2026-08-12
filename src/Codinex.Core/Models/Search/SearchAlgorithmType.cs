namespace Codinex.Core.Models.Search;

/// <summary>
/// Identifies a string-search algorithm implementation.
/// </summary>
public enum SearchAlgorithmType
{
    /// <summary>
    /// Let the <c>ISearchAlgorithmSelector</c> choose an algorithm based on the request shape.
    /// </summary>
    Auto,

    Naive,

    Kmp,

    BoyerMoore,

    BoyerMooreHorspool,

    RabinKarp,

    TwoWay,

    Bitap,

    AhoCorasick,

    Bndm,

    Fuzzy
}
