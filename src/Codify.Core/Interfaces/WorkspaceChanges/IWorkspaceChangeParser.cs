using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;

namespace Codify.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Generates workspace changes from an AI response.
/// </summary>
public interface IWorkspaceChangeParser
{
    Task<WorkspaceChangeSet> ParseAsync(
        string response,
        CancellationToken cancellationToken);
}