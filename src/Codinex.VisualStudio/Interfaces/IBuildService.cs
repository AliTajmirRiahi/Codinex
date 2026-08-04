using System.Threading;
using System.Threading.Tasks;
using Codinex.VisualStudio.Models;

namespace Codinex.VisualStudio.Interfaces;

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

    /// <summary>
    /// Builds a project in the current solution by name.
    /// </summary>
    Task<BuildResult> BuildProjectAsync(
        string projectName,
        CancellationToken cancellationToken);
}