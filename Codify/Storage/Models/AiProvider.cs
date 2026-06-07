using System.Collections.Generic;

namespace Codify.Storage.Models
{
    public class AiProvider
    {
        public string Id { get; set; } = ""; // "gapgpt", "openai"
        public string Name { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public List<AiModel> Models { get; set; } = new List<AiModel>();
        public bool IsEnabled { get; set; } = false;
    }
}