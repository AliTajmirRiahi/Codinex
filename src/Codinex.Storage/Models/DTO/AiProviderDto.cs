
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

    public static ProviderSettingsUpdateResult Saved(string message = "Settings saved successfully.")
    {
        return new ProviderSettingsUpdateResult
        {
            Success = true,
            IsAvailable = true,
            Message = message
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