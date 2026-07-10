using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace Codify.VisualStudio.Interfaces;

/// <summary>
/// Sends messages from the host application back to the WebView UI.
/// </summary>
public interface IWebViewClient
{
    void Initialize(WebView2 webView);
    /// <summary>
    /// Posts a message to the WebView UI.
    /// </summary>
    Task PostMessageAsync(object message);
}