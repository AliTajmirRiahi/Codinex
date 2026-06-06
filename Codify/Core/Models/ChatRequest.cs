namespace Codify.Core.Models;

/// <summary>
/// Represents a chat request coming from the WebView UI.
/// </summary>
public sealed class ChatRequest
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; }

    public ChatRequest() { }

    public ChatRequest(string type, string payload)
    {
        Type = type;
        Payload = payload;
    }
}