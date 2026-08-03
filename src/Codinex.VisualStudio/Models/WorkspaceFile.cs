
namespace Codinex.VisualStudio.Models
{
    public enum WorkspaceMatchType
    {
        Unknown = 0,

        FileName,

        RelativePath,

        Folder,

        Content,

        Symbol
    }
    public sealed class WorkspaceFile
    {
        public string Name { get; set; }

        public string RelativePath { get; set; }

        public string FullPath { get; set; }

        // Optional information for search results

        public int? LineNumber { get; set; }

        public int? Column { get; set; }

        public string Preview { get; set; }

        public WorkspaceMatchType MatchType { get; set; }
    }
}
