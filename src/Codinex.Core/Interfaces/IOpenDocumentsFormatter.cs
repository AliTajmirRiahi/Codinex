using System.Collections.Generic;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
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