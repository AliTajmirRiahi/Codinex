namespace Codinex.Storage.Models
{
    public class CodinexSettings
    {
        // Automatically include the current active document as message context.
        public bool AutoAddActiveDocumentToMessage { get; set; }

        // Show assistant responses as they are generated.
        public bool EnableStreamingChat { get; set; } = true;

        // Enables preprocessing prompts through a local AI provider before sending them to the main chat model.
        public bool EnablePreprocessorAi { get; set; }

        // Local provider id used by the prompt preprocessor.
        public string PreprocessorAiProviderId { get; set; } = string.Empty;

        // Local model id used by the prompt preprocessor.
        public string PreprocessorAiModelId { get; set; } = string.Empty;
    }
}