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
    private readonly WebView2 _webView;
    private readonly IJsonSerializer _serializer;

    public WebViewClient(WebView2 webView, IJsonSerializer serializer)
    {
        _webView = webView;
        _serializer = serializer;
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