using System.Collections.Generic;
using Codinex.Core.Conversation;

namespace Codinex.Core.Models
{
    public sealed class ChatMessageBuildResult
    {
        public IReadOnlyList<ChatMessage> Messages { get; set; }

        public ConversationProviderRole ProviderRole { get; set; } = ConversationProviderRole.Primary;

        public string ProviderId { get; set; }

        public string ProviderName { get; set; }

        public string ModelId { get; set; }

        public string ModelName { get; set; }

        public ChatMessageRequestContext Context { get; set; } = new();
    }
}