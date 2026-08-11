using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges;

/// <summary>
/// Applies a matched text change to file content by replacing the matched span.
/// </summary>
[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
public sealed class TextChangeApplier : ITextChangeApplier
{
    public string Apply(
        string content,
        TextChangeMatchResult match,
        TextFileChange change)
    {
        return change.Operation switch
        {
            TextChangeOperations.InsertBefore => content
                .Insert(match.StartIndex, change.Content),

            TextChangeOperations.InsertAfter => content
                .Insert(match.StartIndex + match.Length, change.Content),

            TextChangeOperations.Delete => content
                .Remove(match.StartIndex, match.Length),

            _ => content
                .Remove(match.StartIndex, match.Length)
                .Insert(match.StartIndex, change.Content)
        };
    }
}
