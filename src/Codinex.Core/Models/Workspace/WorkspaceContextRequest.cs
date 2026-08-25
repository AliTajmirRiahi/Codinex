using System.Collections.Generic;
using Codinex.Core.Models.Chat;
using Codinex.Core.Models.References;

namespace Codinex.Core.Models.Workspace
{
    public sealed class WorkspaceContextRequest
    {
        public IReadOnlyList<ChatMessage> Conversation { get; set; }

        public IReadOnlyList<ReferenceItem> References { get; set; }

        public string AgentId { get; set; }

        public IReadOnlyList<string> ContextsNeeded { get; set; }
    }
}
