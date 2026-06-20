namespace Codify.Core.Models;

/// <summary>
/// Represents message types exchanged between WebView UI and the host application.
/// </summary>
public static class WebViewMessageType
{
    // From JS
    public const string Ready = "READY";
    public const string SendMessage = "SEND_MESSAGE";
    public const string InitState = "INIT_STATE";
    public const string SelectProvider = "SELECT_PROVIDER";
    public const string SelectModel = "SELECT_MODEL";
    public const string CancelGeneration = "CANCEL_GENERATION";
    public const string UpdateSettings = "UPDATE_SETTINGS";
    public const string UiError = "UI_ERROR";
    public const string SelectChat = "SELECT_CHAT";
    public const string NewChat = "NEW_CHAT";
    public const string DeleteChat = "DELETE_CHAT";

    // To JS
    public const string InitData = "INIT_DATA";
    public const string AiResponse = "AI_RESPONSE";
    public const string StreamChunk = "STREAM_CHUNK";
    public const string Error = "ERROR";
    public const string SetLoading = "SET_LOADING";
    public const string SelectChatApproved = "SELECT_CHAT_APPROVED";
    public const string ChatTitleChanged = "CHAT_TITLE_CHANGED";
}