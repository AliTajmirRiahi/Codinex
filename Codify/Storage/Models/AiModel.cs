namespace Codify.Storage.Models
{
    public class AiModel
    {
        public string Id { get; set; } = ""; // e.g. "gpt-4o"
        public string Name { get; set; } = ""; // e.g. "GPT 4o"
        public int TokenLimit { get; set; } = 4096;
        public bool SupportsImages { get; set; } = false;
        public bool SupportsTools { get; set; } = false;
        public bool IsSelected { get; set; } = false; // For UI list selection
    }
}