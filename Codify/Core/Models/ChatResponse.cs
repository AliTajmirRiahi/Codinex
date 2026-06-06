namespace Codify.Core.Models;

/// <summary>
/// Represents a chat response sent back to the WebView UI.
/// </summary>
public sealed class ChatResponse
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;

    public ChatResponse() { }

    public ChatResponse(string type, string payload)
    {
        Type = type;
        Payload = payload;
    }
}