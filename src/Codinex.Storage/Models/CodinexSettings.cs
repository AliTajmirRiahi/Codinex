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

        // Shows an in-chat warning before sending a request whose serialized payload exceeds PromptSizeWarningKb.
        public bool EnablePromptSizeWarning { get; set; } = true;

        // Serialized request payload size, in KB, at or above which the large-prompt warning is shown.
        public int PromptSizeWarningKb { get; set; } = 200;

        // Shows a popup near the system tray clock when a change review opens or a task finishes
        // while Visual Studio is not focused (or is minimized).
        public bool EnableBackgroundToast { get; set; } = true;

        // How long a background-task toast stays visible before auto-dismissing, in seconds.
        public int ToastAutoDismissSeconds { get; set; } = 8;

        // Local provider id used by the prompt preprocessor.
        public string PreprocessorAiProviderId { get; set; } = string.Empty;

        // Local model id used by the prompt preprocessor.
        public string PreprocessorAiModelId { get; set; } = string.Empty;

        // Commit message system prompt
        public string CommitMessageSystemPrompt { get; set; } = """
                                                                
                                                                Format (Conventional Commits):
                                                                - Line 1: "<Type>: <short summary>", capitalized type (Fix, Feat, Refactor, Perf,
                                                                  Docs, Test, Chore, Style, Build, Ci), imperative mood, no trailing period, at
                                                                  most 72 characters.
                                                                - Write a body for every change EXCEPT a single trivial edit (one file, one
                                                                  obvious concern such as a typo, version bump, or one-line fix). When in doubt,
                                                                  write the body.
                                                                - Body structure:
                                                                  1. One lead-in line: a single sentence naming the area of the codebase touched
                                                                     and the overall goal of the change.
                                                                  2. A blank line.
                                                                  3. 3 to 7 bullet points covering the key changes. Each bullet may run 1-3
                                                                     sentences. Name the concrete files, functions, classes, methods, or CSS
                                                                     selectors that changed; say what changed; and state why only when the
                                                                     reason is visible in the diff. Group related edits into one bullet rather
                                                                     than listing every hunk.
                                                                  4. A blank line, then one closing sentence describing the overall structural
                                                                     or user-visible impact of the change.
                                                                  Each bullet is its own line, indented with 7 spaces before the "- ", and
                                                                  starts with a capital letter, e.g.:
                                                                
                                                                  Fix: handle X separately
                                                                
                                                                         - Rework `renderDiff` in changeReviewController.js to build separate
                                                                           leftSide and rightSide containers instead of one flat grid.
                                                                         - Add `mirrorVScroll` to keep the two columns row-aligned.
                                                                
                                                                  Add two enter characters at the end of the message.
                                                                
                                                                Rules:
                                                                - Base the message only on the diff content. Never invent changes, motivation,
                                                                  or file names that are not present in the diff.
                                                                - Prefer specific identifiers from the diff (symbol names, paths, selectors)
                                                                  over vague phrases like "some logic" or "various files".
                                                                - Use real newline characters between the summary, the lead-in, the blank
                                                                  lines, and each bullet — never join them with spaces or dashes on one line.
                                                                - Never wrap the output in Markdown code fences or quotes.
                                                                - Output only the commit message text (summary, body, closing sentence). No
                                                                  preamble and no commentary other than that closing sentence.
                                                                - Never add any explanation, preamble, or trailing commentary — output only the commit message text.
                                                                """;
    }
}