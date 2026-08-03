
namespace Codify.Core.Models
{
    public sealed class WorkspaceEntry
    {
        public string Name { get; set; }

        public string RelativePath { get; set; }

        public string FullPath { get; set; }

        public WorkspaceEntryType Type { get; set; }
    }
}
