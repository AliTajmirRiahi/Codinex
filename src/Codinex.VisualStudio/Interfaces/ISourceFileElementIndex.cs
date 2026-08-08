using System.Collections.Generic;
using Codinex.VisualStudio.Models;

namespace Codinex.VisualStudio.Interfaces;

public interface ISourceFileElementIndex
{
    void UpdateFile(
        string filePath,
        IReadOnlyCollection<SourceFileElement> elements);

    bool TryGetElement(
        string elementId,
        out SourceFileElement element);
}
