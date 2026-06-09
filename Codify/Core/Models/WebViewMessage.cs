
using Newtonsoft.Json.Linq;

namespace Codify.Core.Models;

/// <summary>
/// Represents a chat request coming from the WebView UI.
/// </summary>
public sealed class WebViewMessage
{
    public string Type { get; set; } = string.Empty;
    // IMPORTANT: Use JObject instead of string
    public JObject Payload { get; set; } = new JObject();

    public WebViewMessage() { }

    public WebViewMessage(string type, JObject payload)
    {
        Type = type;
        Payload = payload;
    }
}