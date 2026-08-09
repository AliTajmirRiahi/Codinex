namespace Codinex.Core.Models;

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
    public const string SelectGroup = "SELECT_GROUP";
    public const string NewChat = "NEW_CHAT";
    public const string UpdateChat = "UPDATE_CHAT";
    public const string DeleteChat = "DELETE_CHAT";
    public const string NewGroup = "NEW_GROUP";
    public const string UpdateGroup = "UPDATE_GROUP";
    public const string DeleteGroup = "DELETE_GROUP";
    public const string OpenExternalLink = "OPEN_EXTERNAL_LINK";
    public const string OpenReferenceFile = "OPEN_REFERENCE_FILE";
    public const string RefreshProviderModels = "REFRESH_PROVIDER_MODELS";
    public const string SaveSettings = "SAVE_SETTINGS";
    public const string SaveSolutionInstruction = "SAVE_SOLUTION_INSTRUCTION";

    // To JS
    public const string InitData = "INIT_DATA";
    public const string OpenProviderManager = "OPEN_PROVIDER_MANAGER";
    public const string AiResponse = "AI_RESPONSE";
    public const string StreamChunk = "STREAM_CHUNK";
    public const string ThinkingStarted = "THINKING_STARTED";
    public const string ThinkingChunk = "THINKING_CHUNK";
    public const string ThinkingCompleted = "THINKING_COMPLETED";
    public const string Error = "ERROR";
    public const string SetLoading = "SET_LOADING";
    public const string SelectModelApproved = "SELECT_MODEL_APPROVED";
    public const string SelectChatApproved = "SELECT_CHAT_APPROVED";
    public const string SelectGroupApproved = "SELECT_GROUP_APPROVED";
    public const string ChangeModelSettingApproved = "CHANGE_MODEL_SETTING_APPROVED";
    public const string ChangeModelSettingRejected = "CHANGE_MODEL_SETTING_REJECTED";
    public const string ChatTitleChanged = "CHAT_TITLE_CHANGED";
    public const string ActiveDocumentChanged = "ACTIVE_DOCUMENT_CHANGED";
    public const string InputLanguageChanged = "INPUT_LANGUAGE_CHANGED";
    public const string StatusChanged = "STATUS_CHANGED";
    public const string ProviderModelsRefreshed = "PROVIDER_MODELS_REFRESHED";
    public const string SettingsSaved = "SETTINGS_SAVED";
    public const string SolutionInstructionSaved = "SOLUTION_INSTRUCTION_SAVED";

}