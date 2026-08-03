using System.Collections.Generic;

namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Represents the complete context that will be injected into the LLM prompt.
    /// </summary>
    public sealed class PromptContext
    {
        public IList<PromptContextSection> Sections { get; set; }
            = new List<PromptContextSection>();
    }
}