using System.Collections.Generic;

namespace Codify.Core.Models;

/// <summary>
/// Represents a chat response sent back to the WebView UI.
/// </summary>
public sealed class ChatResponse
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;

    // Extra information from AI or system
    public Dictionary<string, object> Meta { get; }

    public ChatResponse()
    {

    }

    public ChatResponse(string type, string payload, Dictionary<string, object> meta = null)
    {
        Type = type;
        Payload = payload;
        Meta = meta;
    }
}

