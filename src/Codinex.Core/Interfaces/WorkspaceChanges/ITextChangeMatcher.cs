using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Core.Interfaces.WorkspaceChanges;

/// <summary>
/// Matches a text change against a file content.
/// </summary>
public interface ITextChangeMatcher
{
    TextChangeMatchResult Match(
        string content,
        TextFileChange change);

    /// <summary>
    /// Matches arbitrary locator text (e.g. a TextFileChange's Search or Target value)
    /// against file content, independent of any specific TextFileChange.
    /// </summary>
    TextChangeMatchResult MatchText(
        string content,
        string text);
}