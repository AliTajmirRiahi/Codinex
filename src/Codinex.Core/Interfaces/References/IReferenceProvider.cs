using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.References
{
    /// <summary>
    /// Defines a provider that supplies suggestions for context menu triggers (e.g., #).
    /// </summary>
    public interface IReferenceProvider
    {
        Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync();
    }
}
