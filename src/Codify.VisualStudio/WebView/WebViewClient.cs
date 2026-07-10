using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.VisualStudio.Interfaces;
using Microsoft.Web.WebView2.Wpf;

namespace Codify.VisualStudio.WebView;

/// <summary>
/// WebView2-backed implementation of IWebViewClient.
/// </summary>
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