using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Codify.Core.Models
{
    public sealed class ChatMessageBuildResult
    {
        public IReadOnlyList<ChatMessage> Messages { get; set; }

        public ChatMessageRequestContext Context { get; set; } = new();
    }
}