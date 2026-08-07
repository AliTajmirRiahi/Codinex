namespace Codinex.Core.Conversation;

/// <summary>
/// Identifies which AI provider role should execute a conversation request.
/// </summary>
public enum ConversationProviderRole
{
    Primary = 0,
    Preprocessor = 1
}
