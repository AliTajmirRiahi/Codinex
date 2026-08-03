using System.Collections.Generic;
using Codinex.VisualStudio.Models;
using Codinex.VisualStudio.Models.Tools.SearchProject;

namespace Codinex.VisualStudio.Interfaces
{
    public interface IWorkspaceSearchService
    {
        // Find files by file name or relative path.
        IReadOnlyList<WorkspaceFile> FindFiles(string query);

        // Find files by extension (e.g. ".cs", ".json").
        IReadOnlyList<WorkspaceFile> FindByExtension(string extension);

        // Find files using a wildcard pattern (e.g. "*.cs").
        IReadOnlyList<WorkspaceFile> FindByPattern(string pattern);

        // Search for text inside workspace files.
        IReadOnlyList<WorkspaceFile> SearchText(string text);

        // Search using a regular expression.
        IReadOnlyList<WorkspaceFile> SearchRegex(string pattern);

        IReadOnlyList<WorkspaceFile> Search(
            string query,
            SearchProjectType type);
    }
}