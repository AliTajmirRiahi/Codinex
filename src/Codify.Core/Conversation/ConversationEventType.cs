namespace Codify.Core.Conversation;

/// <summary>
/// Represents the type of activity that occurs during a conversation.
/// </summary>
public enum ConversationEventType
{
    Unknown = 0,
    TextDelta = 1,
    ThinkingStarted = 2,
    ThinkingUpdated = 3,
    ThinkingCompleted = 4,
    ToolRequested = 5,
    ToolCompleted = 6,
    ConversationCompleted = 7,
    ConversationCancelled = 8,
    ConversationFailed = 9,
    StatusChanged = 10,
}