using System.Collections.Generic;
using System.Threading;
using Codinex.Core.Models.Chat;
using Codinex.Core.Models.References;

namespace Codinex.Core.Conversation;

public sealed class ConversationContext
{
    public ChatAgent Agent { get; }

    public ChatCommand Command { get; }

    public IReadOnlyList<ReferenceItem> References { get; }

    public CancellationToken CancellationToken { get; }
}