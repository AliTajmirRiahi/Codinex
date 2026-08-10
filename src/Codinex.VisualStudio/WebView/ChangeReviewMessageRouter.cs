using System;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
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
    IWorkspaceApprovalService approvalService,
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

                    approvalService.SetDecision(payload.Id, payload.Approved);

                    return Task.CompletedTask;
                }

            default:
                return Task.CompletedTask;
        }
    }
}

public sealed class ChangesetDecisionDto
{
    public Guid Id { get; set; }

    public bool Approved { get; set; }
}
