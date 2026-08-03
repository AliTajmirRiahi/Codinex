namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Represents the result of matching a text change.
/// </summary>
public enum TextChangeMatchStatus
{
    Success,
    SearchNotFound,
    MultipleMatches,
    NoUniqueMatch
}
public sealed class TextChangeMatchResult
{
    public TextChangeMatchStatus Status { get; set; }

    public int MatchCount { get; set; }

    public int StartIndex { get; set; }

    public int Length { get; set; }

    public string MatchedText { get; set; }

    public string Error { get; set; }
}