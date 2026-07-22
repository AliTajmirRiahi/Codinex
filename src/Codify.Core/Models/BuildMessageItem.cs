namespace Codify.Core.Models
{
    /// <summary>
    /// Represents a single build message.
    /// </summary>
    public sealed class BuildMessageItem
    {
        public BuildMessageSeverity Severity { get; set; }

        public string Code { get; set; }

        public string Message { get; set; }

        public string Project { get; set; }

        public string File { get; set; }

        public int Line { get; set; }
    }
}