using System.Collections.Generic;

namespace Codinex.Core.Models.Context
{
    /// <summary>
    /// Represents the current solution context.
    /// </summary>
    public sealed class ProjectContext
    {
        public string SolutionName { get; set; }

        public string SolutionPath { get; set; }

        public string SolutionDirectory { get; set; }

        public IReadOnlyList<string> StartupProjects { get; set; }

        public string Configuration { get; set; }

        public string Platform { get; set; }

        public IReadOnlyList<ProjectContextItem> Projects { get; set; }
    }
}