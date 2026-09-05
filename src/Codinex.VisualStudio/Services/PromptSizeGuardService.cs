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
///
/// Each Continue raises that chat's effective threshold by one base step
/// (<c>PromptSizeWarningKb</c>): 200 KB -> 400 -> 600 -> ..., so the user is not re-prompted
/// on every subsequent request, only when the payload grows past the new limit.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class PromptSizeGuardService(
    IWebViewClient webViewClient,
    SettingsManager settingsManager) : IPromptSizeGuard
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pending = new();

    // chatId -> how many times the user has pressed Continue for this chat.
    private readonly ConcurrentDictionary<string, int> _continuesByChat = new();

    public async Task<bool> ConfirmAsync(int payloadByteCount, string chatId, CancellationToken cancellationToken)
    {
        var settings = settingsManager.Settings;

        if (settings is not { EnablePromptSizeWarning: true })
        {
            return true;
        }

        var baseKb = Math.Max(1, settings.PromptSizeWarningKb);

        var continues = !string.IsNullOrEmpty(chatId) && _continuesByChat.TryGetValue(chatId, out var c) ? c : 0;
        var effectiveKb = baseKb * (continues + 1);

        if (payloadByteCount < (long)effectiveKb * 1024)
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
                    ThresholdKb = effectiveKb
                }
            });

            var proceed = await tcs.Task;

            if (proceed && !string.IsNullOrEmpty(chatId))
            {
                _continuesByChat.AddOrUpdate(chatId, 1, (_, value) => value + 1);
            }

            return proceed;
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

    public void ResetEscalation() => _continuesByChat.Clear();
}
