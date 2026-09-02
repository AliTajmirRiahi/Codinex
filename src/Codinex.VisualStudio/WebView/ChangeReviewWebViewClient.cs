using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.VisualStudio.Interfaces;

#pragma warning disable VSTHRD003, VSTHRD001 // vs-threading analyzers suppressed project-wide for the VS-integration layer; call sites are audited manually.

namespace Codinex.VisualStudio.WebView;

/// <summary>
/// WebView2-backed implementation of IChangeReviewWebViewClient.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
public sealed class ChangeReviewWebViewClient(IJsonSerializer serializer) : IChangeReviewWebViewClient
{
    private WebView2 _webView;

    // Unlike the chat tool window (open for the lifetime of the IDE), this tool window
    // is created on demand, and PostWebMessageAsJson silently drops messages sent
    // before the page has navigated and its script is listening. So a caller must
    // wait for two things, in order: the host control assigning its WebView2
    // (Initialize), and the page itself confirming it's ready (NotifyViewReady,
    // driven by the CHANGESET_VIEW_READY message the JS sends once loaded).
    private TaskCompletionSource<bool> _hostReadyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private TaskCompletionSource<bool> _viewReadyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Initialize(WebView2 webView)
    {
        // A closed-and-reopened tool window gets a fresh WebView2 (and thus a fresh
        // page load), so any prior readiness signal no longer applies.
        if (_webView != null && !ReferenceEquals(_webView, webView))
        {
            _hostReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _viewReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        _webView = webView;
        _hostReadyTcs.TrySetResult(true);
    }

    public void NotifyViewReady()
    {
        _viewReadyTcs.TrySetResult(true);
    }

    public async Task PostMessageAsync(object message)
    {
        await _hostReadyTcs.Task;
        await _viewReadyTcs.Task;

        var json = serializer.Serialize(message);

        _webView.Dispatcher.Invoke(() =>
        {
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        });
    }
}
