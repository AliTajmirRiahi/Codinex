using System.Collections.Generic;
using Codinex.Core.Conversation;
using Codinex.Core.Models.Chat;

namespace Codinex.Core.Models.Tools
{
    public sealed class ToolRequestedPayload
    {
        public IReadOnlyList<ToolRequest> Requests  { get; set; }

        public ChatMessage AssistantMessage { get; set; }
    }
}
