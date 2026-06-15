using System;
using System.Collections.Generic;

namespace Codify.Core.Models
{
    public class ChatSessionDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ProviderId { get; set; }
        public string ModelId { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
