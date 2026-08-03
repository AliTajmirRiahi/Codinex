using System.Collections.Generic;

namespace Codify.Core.Models
{
    /// <summary>
    /// Context model produced after chat messages are built.
    /// Contains request metadata that may still be needed after message construction,
    /// such as the selected command, selected agent, and selected references.
    /// Unlike <c>ChatMessageBuildRequest</c>, this model does not contain the user's draft text
    /// or conversation history, because that information is already embedded in the final chat messages.
    /// </summary>
    public sealed class ChatMessageRequestContext
    {
        public ChatCommand SelectedCommand { get; set; }

        public ChatAgent SelectedAgent { get; set; }

        public IReadOnlyList<ReferenceItem> SelectedReferences { get; set; }

        public static ChatMessageRequestContext CreateChatMessageRequestContextWithoutMetaData(ChatMessageRequestContext context)
        {
            var selectedReferences = (new List<ReferenceItem>(context.SelectedReferences));

            selectedReferences.ForEach(p => p.Metadata = null);

            return new ChatMessageRequestContext()
            {
                SelectedCommand = context.SelectedCommand,
                SelectedAgent = context.SelectedAgent,
                SelectedReferences = selectedReferences
            };
        }
    }
}