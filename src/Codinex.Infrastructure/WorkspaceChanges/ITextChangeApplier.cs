using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges;

/// <summary>
/// Applies a matched text change to file content.
/// </summary>
public interface ITextChangeApplier
{
    string Apply(
        string content,
        TextChangeMatchResult match,
        TextFileChange change);
}
