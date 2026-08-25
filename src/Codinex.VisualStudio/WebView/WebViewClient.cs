using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.WebView;

/// <summary>
/// WebView2-backed implementation of IWebViewClient.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
public sealed class WebViewClient(IJsonSerializer serializer) : IWebViewClient
{
    private WebView2 _webView;

    // Add this method to connect the UI element later
    public void Initialize(WebView2 webView)
    {
        _webView = webView;
    }

    public Task PostMessageAsync(object message)
    {
        var json = serializer.Serialize(message);

        _webView.Dispatcher.Invoke(() =>
        {
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        });

        return Task.CompletedTask;
    }
}