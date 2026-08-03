using System;
using System.Collections.Generic;
using Codify.Core.Models;
using Codify.Storage.Commons;

namespace Codify.Storage.Models
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
        public List<ChatMessage> Messages { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsNewChat => string.IsNullOrWhiteSpace(Id) || (Messages.Count == 0 && Title == Statics.NewChatTitle);
    }

}
