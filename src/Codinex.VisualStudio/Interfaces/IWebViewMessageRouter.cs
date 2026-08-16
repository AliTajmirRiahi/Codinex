using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.VisualStudio.Interfaces;

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

    /// <summary>
    /// Adds the given selection as a reference chip in the composer, without sending anything.
    /// Used by the "Add to Chat" editor context menu command.
    /// </summary>
    Task SendSelectedCodeReferenceAsync(ReferenceItem selection);

    /// <summary>
    /// Switches to the default conversation group, starts a new chat there, and asks the
    /// WebView to attach the selection plus the "/describe" command and auto-send it.
    /// Used by the "Describe" editor context menu command.
    /// </summary>
    Task RunDescribeOnSelectionAsync(ReferenceItem selection);
}