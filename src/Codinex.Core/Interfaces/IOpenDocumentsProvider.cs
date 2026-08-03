using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Provides information about the currently open documents.
    /// </summary>
    public interface IOpenDocumentsProvider
    {
        Task<IReadOnlyList<ReferenceItem>> GetOpenDocumentsAsync(
            CancellationToken cancellationToken);
    }
}