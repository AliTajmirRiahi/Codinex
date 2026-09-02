using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Codinex.Core.Models.Chat
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

        public JObject Data { get; set; }

        public IReadOnlyList<ToolCall> ToolCalls { get; set; }

        public string ToolCallId { get; set; }

        public ChatMessageRequestContext Context { get; set; }

        public string ProviderId { get; set; }

        public string ProviderName { get; set; }

        public string ModelId { get; set; }

        public string ModelName { get; set; }

        public bool IsPreprocessorAnswer { get; set; }

        /// <summary>
        /// Id of the chat turn this message belongs to. Used to locate the recorded
        /// prompt payload folder at
        /// %LocalAppData%\Codinex\prompts\chat_&lt;chatId&gt;\&lt;ChatMessageId&gt;.
        /// Only assistant messages carry it; older history may leave it null.
        /// </summary>
        public string ChatMessageId { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class ToolCall
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public JObject Arguments { get; set; }
    }
}
