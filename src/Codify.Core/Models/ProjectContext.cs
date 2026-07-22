using System.Collections.Generic;

namespace Codify.Core.Models
{
    /// <summary>
    /// Represents the current solution context.
    /// </summary>
    public sealed class ProjectContext
    {
        public string SolutionName { get; set; }

        public string ActiveProject { get; set; }

        public IReadOnlyList<ProjectContextItem> Projects { get; set; }
    }
}