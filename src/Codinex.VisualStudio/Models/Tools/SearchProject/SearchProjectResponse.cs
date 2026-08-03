using System.Collections.Generic;


namespace Codinex.VisualStudio.Models.Tools.SearchProject;

public sealed class SearchProjectResponse
{
    public int Count { get; set; }

    public IReadOnlyList<WorkspaceFile> Results { get; set; }
}
