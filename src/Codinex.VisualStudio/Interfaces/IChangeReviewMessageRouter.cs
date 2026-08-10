using System.Threading.Tasks;

namespace Codinex.VisualStudio.Interfaces;

/// <summary>
/// Routes incoming messages from the Code Changes review WebView UI.
/// </summary>
public interface IChangeReviewMessageRouter
{
    /// <summary>
    /// Handles a raw message received from the Code Changes review WebView.
    /// </summary>
    /// <param name="messageJson">Raw JSON payload from WebView.</param>
    Task HandleMessageAsync(string messageJson);
}
