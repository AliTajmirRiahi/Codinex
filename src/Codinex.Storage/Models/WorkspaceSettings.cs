namespace Codinex.Storage.Models
{
    public class WorkspaceSettings
    {
        // System instruction applied to every chat in this solution, regardless of conversation group.
        public string SolutionInstruction { get; set; } = string.Empty;

        // Semicolon-separated directory names to exclude from workspace search in this solution (e.g. "bin;obj;node_modules").
        public string ExcludeDirectories { get; set; } = string.Empty;

        // Semicolon-separated file name patterns to exclude from workspace search in this solution (e.g. "*.dll;secrets.json").
        public string ExcludeFiles { get; set; } = string.Empty;
    }
}
