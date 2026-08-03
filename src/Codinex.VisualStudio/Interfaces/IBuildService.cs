using System.Threading;
using System.Threading.Tasks;
using Codify.VisualStudio.Models;

namespace Codify.VisualStudio.Interfaces;

/// <summary>
/// Provides solution build operations.
/// </summary>
public interface IBuildService
{
    /// <summary>
    /// Builds the current solution.
    /// </summary>
    Task<BuildResult> BuildSolutionAsync(
        CancellationToken cancellationToken);
}