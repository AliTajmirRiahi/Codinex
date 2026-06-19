using Codify.Infrastructure.Commons;
using System;
using System.Collections.Generic;

namespace Codify.Core.Models
{
    public class ChatSessionDocument
    {
        private string _title;


        public string Id { get; set; }

        public string Title
        {
            get => string.IsNullOrWhiteSpace(_title) ? Statics.NewChatTitle : _title;
            set => _title = value;
        }
        public string ProviderId { get; set; }
        public string ModelId { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
