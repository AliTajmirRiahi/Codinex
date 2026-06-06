using System.Threading.Tasks;

namespace Codify.Core.Abstractions;

/// <summary>
/// Sends messages from the host application back to the WebView UI.
/// </summary>
public interface IWebViewClient
{
    /// <summary>
    /// Posts a message to the WebView UI.
    /// </summary>
    Task PostMessageAsync(object message);
}