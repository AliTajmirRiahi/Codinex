using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Interfaces.WorkspaceChanges;

public interface IWorkspaceChangeValidator
{
    Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceChangeSet workspaceChangeSet,
        CancellationToken cancellationToken = default);
}