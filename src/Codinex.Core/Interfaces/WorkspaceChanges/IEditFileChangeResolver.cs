using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Find + Validate + Plan for every EditFileChange's TextFileChange entries, run before
/// Review is shown. Search is authoritative and must match exactly one location; when it
/// doesn't (not found, or found more than once), Target is tried as a fallback anchor and
/// must itself match exactly one location. The result is the plan Viewer and Applier act on.
/// </summary>
public interface IEditFileChangeResolver
{
    Task<ChangeValidationResult> ResolveAsync(
        WorkspaceChangeSet changeSet,
        CancellationToken cancellationToken = default);
}
