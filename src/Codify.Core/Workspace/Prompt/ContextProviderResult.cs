using System.Collections.Generic;

namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Represents the result returned by a workspace context provider.
    /// </summary>
    public sealed class ContextProviderResult
    {
        public IList<PromptContextItem> Items { get; set; }
            = new List<PromptContextItem>();

        public bool IsCached { get; set; }

        public bool IsEmpty => Items.Count == 0;
    }
}