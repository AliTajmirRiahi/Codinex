namespace Codinex.Core.Models
{
    /// <summary>
    /// Represents a file tracked by Git.
    /// </summary>
    public sealed class GitFileItem
    {
        public string Path { get; set; }

        public GitFileStatus Status { get; set; }

        public bool IsStaged { get; set; }

        /// <summary>
        /// Number of lines added (+).
        /// </summary>
        public int LinesAdded { get; set; }

        /// <summary>
        /// Number of lines removed (-).
        /// </summary>
        public int LinesDeleted { get; set; }

        /// <summary>
        /// The full unified diff text for this file (hunk headers, context, and +/- lines).
        /// </summary>
        public string Diff { get; set; }
    }
}