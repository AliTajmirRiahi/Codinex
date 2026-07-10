namespace Codify.Storage.Models
{
    public class CodifySettings
    {
        // The ID of the currently selected AI Provider (e.g., "openai", "ollama", "custom")
        public string DefaultProviderId { get; set; } = "openai";

        // The API Key (if needed for the provider)
        public string ApiKey { get; set; } = "";

        // The API URL (Important for Local LLMs or Proxies)
        // Default for OpenAI: https://api.openai.com/v1
        // Default for Ollama: http://localhost:11434/v1
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";

        // The Model Name (e.g., "gpt-4o", "codellama", "deepseek")
        public string ModelName { get; set; } = "gpt-4o";
    }
}