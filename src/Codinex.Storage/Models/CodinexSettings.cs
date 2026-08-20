namespace Codinex.Storage.Models
{
    public class CodinexSettings
    {
        // Automatically include the current active document as message context.
        public bool AutoAddActiveDocumentToMessage { get; set; }

        // Show assistant responses as they are generated.
        public bool EnableStreamingChat { get; set; } = true;

        // Bypasses the Code Changes preview and applies workspace changes directly when the solution is under source control.
        public bool ByPassPreviewChangeAndApplyChangeDirectly { get; set; }

        // Enables preprocessing prompts through a local AI provider before sending them to the main chat model.
        public bool EnablePreprocessorAi { get; set; }

        // Local provider id used by the prompt preprocessor.
        public string PreprocessorAiProviderId { get; set; } = string.Empty;

        // Local model id used by the prompt preprocessor.
        public string PreprocessorAiModelId { get; set; } = string.Empty;

        // Commit message system prompt
        public string CommitMessageSystemPrompt { get; set; } = """
                                                                Format (Conventional Commits):
                                                                - Line 1: "<Type>: <short summary>", capitalized type (Fix, Feat, Refactor, Perf, Docs, Test, Chore, Style, Build, Ci), imperative mood, no trailing period, at most 72 characters.
                                                                - If the change is non-trivial (touches more than one concern or file), add a blank line, then up to 4 short bullet points describing the key changes. Skip the body for small/single-purpose changes.
                                                                  Each bullet is its own line, indented with 7 spaces before the "- ", and starts with a capital letter, e.g.:
                                                                  Fix: handle X separately

                                                                         - First change
                                                                         - Second change
                                                                  Add two enter character end of message
                                                                Rules:
                                                                - Base the message only on the diff content. Never invent changes that are not present.
                                                                - Use real newline characters between the summary, the blank line, and each bullet — never join them with spaces or dashes on one line.
                                                                - Never wrap the output in Markdown code fences or quotes.
                                                                - Never add any explanation, preamble, or trailing commentary — output only the commit message text.
                                                                """;
    }
}