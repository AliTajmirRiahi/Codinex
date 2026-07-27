using System.Collections.Generic;
using Codify.Core.Conversation;

namespace Codify.Core.Models
{
    public sealed class ToolRequestedPayload
    {
        public IReadOnlyList<ToolRequest> Requests  { get; set; }

        public ChatMessage AssistantMessage { get; set; }
    }
}
