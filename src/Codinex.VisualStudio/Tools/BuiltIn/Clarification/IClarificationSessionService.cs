using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.Tools.AskUserQuestion;

namespace Codinex.VisualStudio.Tools.BuiltIn.Clarification;

/// <summary>
/// Bridges the "ask_user_question" AI tool with the chat webview: posts the questions to the
/// UI, blocks the calling tool until the user answers (or cancellation unblocks it), and
/// resolves the pending wait when the answer arrives back from the webview.
/// </summary>
public interface IClarificationSessionService
{
    /// <summary>
    /// Shows the questions in the webview and waits for the user's answers. Returns null if the
    /// wait was cancelled (e.g. the user hit Cancel or Visual Studio is shutting down) before an
    /// answer arrived.
    /// </summary>
    Task<List<ClarificationAnswer>> AskAsync(
        string requestId,
        List<ClarificationQuestion> questions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called when the webview posts the user's answers back. Resolves the matching
    /// <see cref="AskAsync"/> call if one is still waiting; returns false otherwise (e.g. the
    /// request already timed out or was answered).
    /// </summary>
    bool SubmitAnswers(string requestId, List<ClarificationAnswer> answers);
}
