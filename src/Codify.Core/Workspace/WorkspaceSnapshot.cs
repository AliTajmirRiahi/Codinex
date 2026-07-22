using System.Collections.Generic;

namespace Codify.Core.Workspace
{
    public sealed class WorkspaceSnapshot
    {
        public string SolutionName { get; set; }

        public string SolutionPath { get; set; }

        public string ActiveDocument { get; set; }

        public IReadOnlyList<string> OpenDocuments { get; set; }
    }
}