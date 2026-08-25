using Codinex.Core.Models.Workspace;

namespace Codinex.VisualStudio.Models.Tools.ListDirectory;

public sealed class DirectoryEntry
{
    public string Name { get; set; }

    public string Path { get; set; }

    public WorkspaceEntryType Type { get; set; }
}