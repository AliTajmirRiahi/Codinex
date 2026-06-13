
using System.Collections.Generic;

namespace Codify.Storage.Models.Dtos
{
    public class AiProviderDto
    {
        public string ProviderId { get; set; } = ""; // "gapgpt", "openai"
        public string ApiKey { get; set; } = "";
        public List<AiModel> SelectedModels { get; set; } = new List<AiModel>();
        public bool IsEnabled { get; set; } = true;
    }
}
