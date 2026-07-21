using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Codify.Core.Models
{
    //public enum ChatRole
    //{
    //    System,
    //    User,
    //    Assistant,
    //    Tool
    //}
    public class ChatMessage
    {
        public string Role { get; set; }

        public string Content { get; set; }

        public IReadOnlyList<ToolCall> ToolCalls { get; set; }

        public string ToolCallId { get; set; }

        public ChatMessageRequestContext Context { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates whether this request should use streaming output.
        /// Default is false to preserve current behavior.
        /// </summary>
        public bool Stream { get; set; } = true;
    }

    public sealed class ToolCall
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public JObject Arguments { get; set; }
    }
}
