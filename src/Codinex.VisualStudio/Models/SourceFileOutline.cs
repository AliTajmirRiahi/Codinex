using System.Collections.Generic;

namespace Codinex.VisualStudio.Models;

public sealed class SourceFileOutline
{
    public string File { get; set; }

    public string Language { get; set; }

    public IReadOnlyList<SourceFileElement> Elements { get; set; } = [];
}
