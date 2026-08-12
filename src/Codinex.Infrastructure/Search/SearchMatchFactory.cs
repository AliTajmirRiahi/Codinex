using Codinex.Core.Models.Search;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.Search;

/// <summary>
/// Builds <see cref="SearchMatch"/> instances with consistent Range/Score/MatchType wiring.
/// Line/column fields on <see cref="TextRange"/> are left at their default (0) here; the
/// <c>ICodeSearchEngine</c> fills them in once, in a single pass, for every match it returns.
/// </summary>
internal static class SearchMatchFactory
{
    public static SearchMatch CreateExact(
        string text,
        int start,
        int length,
        SearchAlgorithmType algorithm,
        StringSearchOptions options)
    {
        return new SearchMatch
        {
            Range = new TextRange { Start = start, Length = length },
            Score = 1.0,
            Algorithm = algorithm,
            MatchType = SearchOptionsHelper.ResolveMatchType(options),
            MatchedText = text.Substring(start, length)
        };
    }

    public static SearchMatch CreateFuzzy(
        string text,
        int start,
        int length,
        double score,
        SearchAlgorithmType algorithm)
    {
        return new SearchMatch
        {
            Range = new TextRange { Start = start, Length = length },
            Score = score,
            Algorithm = algorithm,
            MatchType = MatchType.Fuzzy,
            MatchedText = text.Substring(start, length)
        };
    }
}
