using System;
using System.Collections.Generic;
using Codinex.Core.Models;
using Codinex.Storage.Commons;

namespace Codinex.Storage.Models
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
        public List<ChatMessage> Messages { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsSelected { get; set; }

        public bool IsNewChat => string.IsNullOrWhiteSpace(Id) || (Messages.Count == 0 && Title == Statics.NewChatTitle);
    }

}
