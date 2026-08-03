using System.Collections.Generic;


namespace Codify.VisualStudio.Models.Tools.SearchProject;

public sealed class SearchProjectResponse
{
    public int Count { get; set; }

    public IReadOnlyList<WorkspaceFile> Results { get; set; }
}
