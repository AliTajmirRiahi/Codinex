using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.WebView;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Tools.BuiltIn.Workspace;

namespace Codinex.VisualStudio.WebView;

/// <summary>
/// Routes messages from the Code Changes review WebView UI.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
public sealed class ChangeReviewMessageRouter(
    IJsonSerializer serializer,
    IPayloadBinder payloadBinder,
    IChangesetSessionService changesetSessionService,
    IChangeReviewWebViewClient changeReviewWebViewClient)
    : IChangeReviewMessageRouter
{
    public Task HandleMessageAsync(string messageJson)
    {
        if (string.IsNullOrWhiteSpace(messageJson))
            throw new InvalidOperationException("Empty message received.");

        var request = serializer.Deserialize<WebViewMessageRequest>(messageJson);

        if (request is null)
            throw new InvalidOperationException("Message could not be parsed.");

        switch (request.Type)
        {
            case WebViewMessageType.ChangesetViewReady:
                {
                    changeReviewWebViewClient.NotifyViewReady();

                    return Task.CompletedTask;
                }

            case WebViewMessageType.ChangesetDecision:
                {
                    var payload = payloadBinder.Bind<ChangesetDecisionDto>(request.Payload);

                    return changesetSessionService.SubmitDecisionAsync(payload.Id, new ChangesetDecision
                    {
                        FileDecisions = payload.Files ?? new Dictionary<string, bool>(),
                        Reason = payload.Reason
                    });
                }

            default:
                return Task.CompletedTask;
        }
    }
}

public sealed class ChangesetDecisionDto
{
    public Guid Id { get; set; }

    /// <summary>Change path (see <see cref="WorkspaceChangePathResolver"/>) -> approved.</summary>
    public Dictionary<string, bool> Files { get; set; } = new();

    public string Reason { get; set; }
}
