
using System.Collections.Generic;
using Codinex.Core.Models.AI;

namespace Codinex.Storage.Models.DTO;

public class AiProviderDto
{
    public string ProviderId { get; set; } = ""; // "gapgpt", "openai"
    public string ApiKey { get; set; } = "";
    public List<AiModel> SelectedModels { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
}

public class ProviderSettingsUpdateResult
{
    public bool Success { get; set; }
    public bool IsAvailable { get; set; }
    public string Message { get; set; } = "";

    /// <summary>
    /// Non-blocking notice shown alongside a successful save, e.g. the provider was stored
    /// but its capabilities could not be verified because it is out of credits or rate limited.
    /// Empty when there is nothing to warn about.
    /// </summary>
    public string Warning { get; set; } = "";

    public static ProviderSettingsUpdateResult Saved(string message = "Settings saved successfully.")
    {
        return new ProviderSettingsUpdateResult
        {
            Success = true,
            IsAvailable = true,
            Message = message
        };
    }

    /// <summary>
    /// The settings were saved, but the provider is not usable right now (e.g. insufficient
    /// credits). The selection is kept; <paramref name="warning"/> tells the user why.
    /// </summary>
    public static ProviderSettingsUpdateResult SavedWithWarning(
        string warning,
        string message = "Settings saved successfully.")
    {
        return new ProviderSettingsUpdateResult
        {
            Success = true,
            IsAvailable = false,
            Message = message,
            Warning = warning
        };
    }

    public static ProviderSettingsUpdateResult Failed(string message, bool isAvailable = true)
    {
        return new ProviderSettingsUpdateResult
        {
            Success = false,
            IsAvailable = isAvailable,
            Message = message
        };
    }
}