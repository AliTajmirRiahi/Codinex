using System.Collections.Generic;

namespace Codinex.Core.Models.Context
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