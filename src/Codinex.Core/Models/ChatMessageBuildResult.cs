using System.Collections.Generic;
using Codinex.Core.Conversation;

namespace Codinex.Core.Models
{
    public sealed class ChatMessageBuildResult
    {
        public IReadOnlyList<ChatMessage> Messages { get; set; }

        public ConversationProviderRole ProviderRole { get; set; } = ConversationProviderRole.Primary;

        public ChatMessageRequestContext Context { get; set; } = new();
    }
}