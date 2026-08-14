using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools.AskUserQuestion;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Tools.BuiltIn.Clarification;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class ClarificationSessionService(IWebViewClient webViewClient) : IClarificationSessionService
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<List<ClarificationAnswer>>> _pending = new();

    public async Task<List<ClarificationAnswer>> AskAsync(
        string requestId,
        List<ClarificationQuestion> questions,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<List<ClarificationAnswer>>(TaskCreationOptions.RunContinuationsAsynchronously);

        _pending[requestId] = tcs;

        using var registration = cancellationToken.Register(() => tcs.TrySetResult(null));

        try
        {
            await webViewClient.PostMessageAsync(new WebViewMessageResponse
            {
                Type = WebViewMessageType.AskUserQuestion,
                Payload = new
                {
                    RequestId = requestId,
                    Questions = questions
                }
            });

            return await tcs.Task;
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    public bool SubmitAnswers(string requestId, List<ClarificationAnswer> answers)
    {
        if (!_pending.TryGetValue(requestId, out var tcs))
            return false;

        return tcs.TrySetResult(answers);
    }
}
