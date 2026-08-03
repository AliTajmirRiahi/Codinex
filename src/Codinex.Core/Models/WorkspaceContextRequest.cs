using System.Collections.Generic;

namespace Codinex.Core.Models
{
    public sealed class WorkspaceContextRequest
    {
        public IReadOnlyList<ChatMessage> Conversation { get; set; }

        public IReadOnlyList<ReferenceItem> References { get; set; }

        public string AgentId { get; set; }
    }
}
