using System.Collections.Generic;
using Codify.Core.Models;

namespace Codify.Core.Workspace.Prompt
{
    public sealed class WorkspaceContextRequest
    {
        public WorkspaceState WorkspaceState { get; set; }

        public IReadOnlyList<ChatMessage> Conversation { get; set; }

        public IReadOnlyList<ReferenceItem> References { get; set; }

        public string AgentId { get; set; }
    }
}
