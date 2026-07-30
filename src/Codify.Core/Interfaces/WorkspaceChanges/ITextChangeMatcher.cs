using Codify.Core.Models.WorkspaceChanges;

namespace Codify.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Matches a text change against a file content.
/// </summary>
public interface ITextChangeMatcher
{
    TextChangeMatchResult Match(
        string content,
        TextFileChange change);
}