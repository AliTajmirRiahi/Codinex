namespace Codify.Core.Models
{
    /// <summary>
    /// Represents a project in the current solution.
    /// </summary>
    public sealed class ProjectContextItem
    {
        public string Name { get; set; }

        public string FullPath { get; set; }

        public string RelativePath { get; set; }

        public string TargetFramework { get; set; }

        public string OutputType { get; set; }

        public string AssemblyName { get; set; }

        public string RootNamespace { get; set; }
    }
}