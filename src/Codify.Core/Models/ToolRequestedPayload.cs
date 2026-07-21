using Codify.Core.Conversation;

namespace Codify.Core.Models
{
    public sealed class ToolRequestedPayload
    {
        public ToolRequest Request { get; set; }

        public ChatMessage AssistantMessage { get; set; }
    }
}
