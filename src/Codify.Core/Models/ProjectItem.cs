namespace Codify.Core.Models
{
    /// <summary>
    /// Represents a project in the current solution.
    /// </summary>
    public sealed class ProjectContextItem
    {
        public string Name { get; set; }

        public string TargetFramework { get; set; }

        public string Language { get; set; }

        public string OutputType { get; set; }
    }
}