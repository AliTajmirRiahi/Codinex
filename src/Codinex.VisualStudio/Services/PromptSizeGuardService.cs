using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.WebView;
using Codinex.Storage.Managers;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Services;

/// <summary>
/// Bridges the large-prompt warning with the chat webview: posts the warning, blocks the
/// calling send until the user answers Continue / Stop (or cancellation unblocks it), and
/// resolves the pending wait when the decision arrives back from the webview. Modeled on
/// <see cref="Tools.BuiltIn.Clarification.ClarificationSessionService"/>.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class PromptSizeGuardService(
    IWebViewClient webViewClient,
    SettingsManager settingsManager) : IPromptSizeGuard
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pending = new();

    public async Task<bool> ConfirmAsync(int payloadByteCount, CancellationToken cancellationToken)
    {
        var settings = settingsManager.Settings;

        if (settings is not { EnablePromptSizeWarning: true })
        {
            return true;
        }

        var thresholdKb = Math.Max(1, settings.PromptSizeWarningKb);

        if (payloadByteCount < thresholdKb * 1024)
        {
            return true;
        }

        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _pending[requestId] = tcs;

        using var registration = cancellationToken.Register(() => tcs.TrySetResult(false));

        try
        {
            await webViewClient.PostMessageAsync(new WebViewMessageResponse
            {
                Type = WebViewMessageType.PromptSizeWarning,
                Payload = new
                {
                    RequestId = requestId,
                    SizeKb = (int)Math.Round(payloadByteCount / 1024d),
                    ThresholdKb = thresholdKb
                }
            });

            return await tcs.Task;
        }
        catch
        {
            // If the webview can't be reached (e.g. the tool window isn't open), don't
            // hard-block the send - fall through and let it proceed.
            return true;
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    public bool SubmitDecision(string requestId, bool proceed)
    {
        if (string.IsNullOrEmpty(requestId) || !_pending.TryGetValue(requestId, out var tcs))
        {
            return false;
        }

        return tcs.TrySetResult(proceed);
    }
}
