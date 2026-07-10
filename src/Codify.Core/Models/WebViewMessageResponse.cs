
using System;

namespace Codify.Core.Models;

/// <summary>
/// Represents a chat request coming from the WebView UI.
/// </summary>
public sealed class WebViewMessageResponse
{
    public string Type { get; set; } = string.Empty;
    // IMPORTANT: Use JObject instead of string
    public dynamic Payload { get; set; }

    public DateTime Timestamp { get; set; }

    public WebViewMessageResponse() { }

    public WebViewMessageResponse(string type, dynamic payload, DateTime timestamp)
    {
        Type = type;
        Payload = payload;
        Timestamp = timestamp;
    }
}