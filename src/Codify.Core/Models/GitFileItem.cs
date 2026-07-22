namespace Codify.Core.Models
{
    /// <summary>
    /// Represents a file tracked by Git.
    /// </summary>
    public sealed class GitFileItem
    {
        public string Path { get; set; }

        public GitFileStatus Status { get; set; }

        public bool IsStaged { get; set; }
    }
}