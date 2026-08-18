using System;

namespace Codinex.Core.Chat
{
    /// <summary>
    /// Thrown when the AI provider fails to generate a commit message (network error,
    /// authentication failure, insufficient credits, etc.). Carries the same human-readable
    /// message the provider's ConversationFailed event reports.
    /// </summary>
    public sealed class CommitMessageProviderException : Exception
    {
        public CommitMessageProviderException(string message)
            : base(message)
        {
        }
    }
}
