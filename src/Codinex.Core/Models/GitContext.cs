using System.Collections.Generic;

namespace Codify.Core.Models
{
    /// <summary>
    /// Represents Git information for the current workspace.
    /// </summary>
    public sealed class GitContext
    {
        public string BranchName { get; set; }

        public IReadOnlyList<GitFileItem> Files { get; set; }
    }
}