using System.Collections.Generic;
using Codinex.VisualStudio.Models;

namespace Codinex.VisualStudio.Interfaces;

public interface ISourceFileElementService
{
    IReadOnlyList<string> SupportedExtensions { get; }

    bool IsSupported(string path);

    string GetLanguage(string path);

    SourceFileOutline GetFileOutline(
        string fullPath,
        string relativePath = null);

    SourceFileElement FindElement(string elementId);
}
