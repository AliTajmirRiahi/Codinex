using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.Extensions;

public static class TextChangeMatchStatusExtensions
{
    public static WorkspaceChangeErrorCode ToWorkspaceChangeErrorCode(
        this TextChangeMatchStatus status)
    {
        return status switch
        {
            TextChangeMatchStatus.SearchNotFound => WorkspaceChangeErrorCode.SearchNotFound,
            TextChangeMatchStatus.MultipleMatches => WorkspaceChangeErrorCode.MultipleMatches,
            TextChangeMatchStatus.NoUniqueMatch => WorkspaceChangeErrorCode.NoUniqueMatch,
            _ => WorkspaceChangeErrorCode.Unknown
        };
    }
}
