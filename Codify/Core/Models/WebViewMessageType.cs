namespace Codify.Core.Models;

/// <summary>
/// Represents message types exchanged between WebView UI and the host application.
/// </summary>
public static class WebViewMessageType
{
    public const string InitData = "INIT_DATA";
    public const string UserInput = "USER_INPUT";
    public const string AiResponse = "AI_RESPONSE";
    public const string Error = "ERROR";
}