using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;
using Newtonsoft.Json.Linq;

namespace Codify.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Generates workspace changes from an AI response.
/// </summary>
public interface IWorkspaceChangeParser
{
    Task<WorkspaceChangeSet> ParseAsync(
        JObject response,
        CancellationToken cancellationToken);
}