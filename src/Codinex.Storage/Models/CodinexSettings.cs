namespace Codinex.Storage.Models
{
    public class CodinexSettings
    {
        // Automatically include the current active document as message context.
        public bool AutoAddActiveDocumentToMessage { get; set; }

        // Show assistant responses as they are generated.
        public bool EnableStreamingChat { get; set; } = true;
    }
}