using System.Collections.Generic;
using Codinex.Core.Models.References;

namespace Codinex.Core.Interfaces.Context
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