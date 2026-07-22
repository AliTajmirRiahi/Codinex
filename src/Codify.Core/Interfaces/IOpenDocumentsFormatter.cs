using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    /// <summary>
    /// Formats open documents into prompt text.
    /// </summary>
    public interface IOpenDocumentsFormatter
    {
        string Format(OpenDocumentsContext context);
    }
}