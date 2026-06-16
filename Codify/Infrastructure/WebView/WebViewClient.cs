using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Codify.Core.Abstractions;
using Microsoft.Web.WebView2.Wpf;

namespace Codify.Infrastructure.WebView;

/// <summary>
/// WebView2-backed implementation of IWebViewClient.
/// </summary>
public sealed class WebViewClient : IWebViewClient
{
    private WebView2 _webView;
    private readonly IJsonSerializer _serializer;

    public WebViewClient(IJsonSerializer serializer)
    {
        _serializer = serializer;
    }

    // Add this method to connect the UI element later
    public void Initialize(WebView2 webView)
    {
        _webView = webView;
    }

    public Task PostMessageAsync(object message)
    {
        var json = _serializer.Serialize(message);

        _webView.Dispatcher.Invoke(() =>
        {
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        });

        return Task.CompletedTask;
    }
}