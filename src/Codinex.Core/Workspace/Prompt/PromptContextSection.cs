using System.Collections.Generic;

namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Represents a logical section of the prompt context.
    /// </summary>
    public sealed class PromptContextSection
    {
        public string Name { get; set; }

        public int Score { get; set; }

        public IList<PromptContextItem> Items { get; set; }
            = new List<PromptContextItem>();
    }
}