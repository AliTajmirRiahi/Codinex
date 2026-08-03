using Codify.Core.Models;
using System.Collections.Generic;

namespace Codify.Core.Interfaces
{
    /// <summary>
    /// Formats open documents into prompt text.
    /// </summary>
    public interface IOpenDocumentsFormatter
    {
        string Format(
            IReadOnlyList<ReferenceItem> documents);
    }
}