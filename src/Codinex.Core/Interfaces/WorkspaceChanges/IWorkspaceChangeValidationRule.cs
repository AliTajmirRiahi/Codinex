using Codify.Core.Models.WorkspaceChanges;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Core.Interfaces.WorkspaceChanges;

public interface IWorkspaceChangeValidationRule
{
    Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default);
}