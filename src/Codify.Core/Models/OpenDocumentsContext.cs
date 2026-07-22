using System.Collections.Generic;

namespace Codify.Core.Models
{
    /// <summary>
    /// Represents the currently open documents.
    /// </summary>
    public sealed class OpenDocumentsContext
    {
        public IReadOnlyList<OpenDocumentItem> Documents { get; set; }
    }
}