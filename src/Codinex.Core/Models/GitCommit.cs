using System;

namespace Codinex.Core.Models
{
    /// <summary>
    /// Represents a single Git commit (changeset).
    /// </summary>
    public sealed class GitCommit
    {
        public string Sha { get; set; }

        public string ShortSha { get; set; }

        public string AuthorName { get; set; }

        public string AuthorEmail { get; set; }

        public DateTimeOffset Date { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// Total number of lines added (+) across all files in the commit.
        /// </summary>
        public int LinesAdded { get; set; }

        /// <summary>
        /// Total number of lines removed (-) across all files in the commit.
        /// </summary>
        public int LinesDeleted { get; set; }
    }
}
