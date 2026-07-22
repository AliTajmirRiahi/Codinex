namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Represents a single piece of prompt context.
    /// </summary>
    public sealed class PromptContextItem
    {
        public string Id { get; set; }

        public PromptContextKind Kind { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public int Score { get; set; }
    }
}