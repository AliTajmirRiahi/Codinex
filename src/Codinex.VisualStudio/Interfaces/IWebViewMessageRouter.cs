using System.Threading.Tasks;

namespace Codify.VisualStudio.Interfaces;

/// <summary>
/// Routes incoming WebView messages to the appropriate application use cases.
/// </summary>
public interface IWebViewMessageRouter
{
    /// <summary>
    /// Handles a raw message received from WebView.
    /// </summary>
    /// <param name="messageJson">Raw JSON payload from WebView.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleMessageAsync(string messageJson);

    /// <summary>
    /// Sends the initial configuration, providers, and models to the WebView.
    /// This is triggered when the UI is ready. It now uses the correct message type from WebViewMessageType.
    /// </summary>
    Task SendInitialDataAsync(bool includeChats = false);
}