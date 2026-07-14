using System.Collections.Generic;
using System.Threading;
using Codify.Core.Models;

namespace Codify.Core.Conversation;

public sealed class ConversationContext
{
    public ChatAgent Agent { get; }

    public ChatCommand Command { get; }

    public IReadOnlyList<ReferenceItem> References { get; }

    public CancellationToken CancellationToken { get; }
}