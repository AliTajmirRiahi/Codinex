using Codinex.Core.Models.Search;

namespace Codinex.Core.Interfaces.Search;

/// <summary>
/// Locates candidate source-code locations for a pattern (typically the Search/Target text of a
/// TextFileChange). The engine only finds and ranks candidates — it never decides which one is
/// correct; that responsibility belongs to the caller's validator.
/// </summary>
public interface ICodeSearchEngine
{
    CodeSearchResult Search(SearchRequest request);
}
