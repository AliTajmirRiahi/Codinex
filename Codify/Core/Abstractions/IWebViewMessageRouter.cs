using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace Codify.Core.Abstractions;

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
}