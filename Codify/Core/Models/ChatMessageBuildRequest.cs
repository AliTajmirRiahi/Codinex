using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Codify.Core.Models
{
    /// <summary>
    /// Input model for <c>ChatMessageBuilder</c>.
    /// Contains the raw request data collected before building the final AI message list,
    /// such as the user's draft text, selected command, selected agent,
    /// selected references, and conversation history.
    /// This model represents the source data used to assemble the final chat payload.
    /// </summary>
    public sealed class ChatMessageBuildRequest
    {
        public string DraftText { get; set; } = string.Empty;

        public ChatCommand SelectedCommand { get; set; }

        public ChatAgent SelectedAgent { get; set; }

        public IReadOnlyList<ReferenceItem> SelectedReferences { get; set; }

        public IReadOnlyList<ChatMessage> ConversationHistory { get; set; }
    }
}