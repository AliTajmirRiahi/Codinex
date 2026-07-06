using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

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

        public IReadOnlyList<ReferenceItem> SelectedReferences { get; set; } = new ArrayBuilder<ReferenceItem>();
    }
}