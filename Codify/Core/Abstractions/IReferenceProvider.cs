using System.Collections.Generic;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Abstractions
{
    /// <summary>
    /// Defines a provider that supplies suggestions for context menu triggers (e.g., #).
    /// </summary>
    public interface IReferenceProvider
    {
        Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync();
    }
}
