using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    /// <summary>
    /// Provides information about the currently open documents.
    /// </summary>
    public interface IOpenDocumentsContextProvider
    {
        Task<OpenDocumentsContext> GetContextAsync(
            CancellationToken cancellationToken);
    }
}