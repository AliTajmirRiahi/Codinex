using System.Collections.Generic;

namespace Codinex.Core.Models.Chat
{
    public sealed class PromptProfileResult
    {
        public int TotalCharacters { get; set; }

        public int EstimatedTokens { get; set; }

        public IReadOnlyList<PromptSectionProfile> Sections { get; set; } = new List<PromptSectionProfile>();
    }
}
