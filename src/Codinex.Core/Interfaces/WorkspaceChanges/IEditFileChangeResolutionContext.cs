using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Ambient carrier for the current changeset's resolved EditFileChange plan (produced by
/// <see cref="IEditFileChangeResolver"/>). Set by the review/apply orchestrator right before
/// applying and read by <c>EditFileChangeHandler</c> purely as a fallback: it still matches
/// on Search first, against the freshly-read file, and only consults this when that fails
/// (e.g. the file changed since the plan was resolved).
/// </summary>
public interface IEditFileChangeResolutionContext
{
    ChangeValidationResult Current { get; set; }
}
