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

        // Semicolon-separated file extensions to always ignore in workspace operations (e.g. ".dll;.exe").
        public string IgnoredExtensions { get; set; } = ".db;.dll;.exe;.pdb;.cache;.log";

        // Semicolon-separated file name suffixes to always ignore in workspace operations (e.g. ".deps.json").
        public string IgnoredFileSuffixes { get; set; } = ".nuget.dgspec.json;.deps.json;.runtimeconfig.json;.AssemblyInfo.cs;.AssemblyAttributes.cs";
    }
}
