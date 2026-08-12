using System.Collections.Generic;
using Codinex.Core.Models.Search;

namespace Codinex.Core.Interfaces.Search;

/// <summary>
/// A string-search algorithm capable of locating multiple patterns in a single pass over the text
/// (e.g. Aho-Corasick). <see cref="SearchMatch.PatternIndex"/> and <see cref="SearchMatch.MatchedPattern"/>
/// identify which pattern each returned match corresponds to.
/// </summary>
public interface IMultiPatternStringSearchAlgorithm : IStringSearchAlgorithm
{
    IReadOnlyList<SearchMatch> SearchMultiple(
        string text,
        IReadOnlyList<string> patterns,
        StringSearchOptions options);
}
