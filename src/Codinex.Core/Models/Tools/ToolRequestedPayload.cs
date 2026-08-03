using System.Collections.Generic;
using Codinex.Core.Conversation;

namespace Codinex.Core.Models.Tools
{
    public sealed class ToolRequestedPayload
    {
        public IReadOnlyList<ToolRequest> Requests  { get; set; }

        public ChatMessage AssistantMessage { get; set; }
    }
}
