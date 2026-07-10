using System;

namespace Codify.Core.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public ChatMessageRequestContext Context { get; set; }
        public string Role { get; set; } // "user", "assistant", "system"
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates whether this request should use streaming output.
        /// Default is false to preserve current behavior.
        /// </summary>
        public bool Stream { get; set; } = true;
    }
}
