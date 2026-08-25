using System.Threading.Tasks;
using Codinex.Core.Models.References;

namespace Codinex.Core.Interfaces.References
{
    /// <summary>
    /// Builds a single file <see cref="ReferenceItem"/> on demand, for consumers (e.g. a workspace
    /// file watcher) that need to react to an individual file rather than re-scanning the solution.
    /// </summary>
    public interface IFileReferenceBuilder
    {
        Task<ReferenceItem> BuildFileReferenceAsync(string filePath);
    }
}
