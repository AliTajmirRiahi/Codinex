namespace Codinex.VisualStudio.Models;

public sealed class SourceFileElement
{
    public string Id { get; set; }

    public string Kind { get; set; }

    public string Name { get; set; }

    public string Signature { get; set; }

    public string Source { get; set; }

    public int Order { get; set; }
}
