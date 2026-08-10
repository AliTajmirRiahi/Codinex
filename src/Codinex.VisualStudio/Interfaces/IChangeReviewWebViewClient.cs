using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace Codinex.VisualStudio.Interfaces;

/// <summary>
/// Sends messages from the host application to the Code Changes review WebView UI.
/// </summary>
public interface IChangeReviewWebViewClient
{
    void Initialize(WebView2 webView);

    /// <summary>
    /// Called once the review page has navigated and its script has registered
    /// a message listener, so PostMessageAsync knows it is safe to send.
    /// </summary>
    void NotifyViewReady();

    /// <summary>
    /// Posts a message to the Code Changes review WebView UI. Waits for the page
    /// to be navigated and ready (see <see cref="NotifyViewReady"/>) first, since
    /// WebView2 silently drops messages posted before the page can receive them.
    /// </summary>
    Task PostMessageAsync(object message);
}
