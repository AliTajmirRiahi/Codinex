using System.Collections.Generic;

namespace Codinex.VisualStudio.Models.Tools.ListDirectory;

public sealed class ListDirectoryResponse
{
    public string Path { get; set; }

    public IReadOnlyList<DirectoryEntry> Entries { get; set; }
}