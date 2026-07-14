using System;
using Newtonsoft.Json.Linq;

namespace Codify.Core.Conversation;

/// <summary>
/// Represents an event emitted during the lifetime of a conversation.
/// </summary>
public sealed class ConversationEvent
{
    /// <summary>
    /// Gets the event type.
    /// </summary>
    public ConversationEventType Type { get; private set; }

    /// <summary>
    /// Gets the structured event payload.
    /// </summary>
    public JToken Payload { get; private set; }

    /// <summary>
    /// Gets the human-readable message associated with the event.
    /// </summary>
    public string DisplayMessage { get; private set; }

    /// <summary>
    /// Gets the event sequence number.
    /// </summary>
    public long Sequence { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp.
    /// </summary>
    public DateTime TimestampUtc { get; private set; }

    private ConversationEvent()
    {
    }

    public static ConversationEvent Create(
        ConversationEventType type,
        JToken payload = null,
        string message = null,
        long sequence = 0)
    {
        return new ConversationEvent
        {
            Type = type,
            Payload = payload,
            DisplayMessage = message,
            Sequence = sequence,
            TimestampUtc = DateTime.UtcNow
        };
    }

    public static ConversationEvent TextDelta(string text, long sequence = 0)
    {
        return Create(
            ConversationEventType.TextDelta,
            JValue.CreateString(text),
            sequence: sequence);
    }

    public static ConversationEvent Status(string message, long sequence = 0)
    {
        return Create(
            ConversationEventType.StatusChanged,
            message: message,
            sequence: sequence);
    }

    public static ConversationEvent ThinkingStarted(long sequence = 0)
    {
        return Create(
            ConversationEventType.ThinkingStarted,
            sequence: sequence);
    }

    public static ConversationEvent ThinkingUpdated(string text, long sequence = 0)
    {
        return Create(
            ConversationEventType.ThinkingUpdated,
            message: text,
            sequence: sequence);
    }

    public static ConversationEvent ThinkingCompleted(long sequence = 0)
    {
        return Create(
            ConversationEventType.ThinkingCompleted,
            sequence: sequence);
    }

    public static ConversationEvent ToolRequested(ToolRequest request, long sequence = 0)
    {
        return Create(
            ConversationEventType.ToolRequested,
            JToken.FromObject(request),
            sequence: sequence);
    }

    public static ConversationEvent ToolCompleted(ToolResult result, long sequence = 0)
    {
        return Create(
            ConversationEventType.ToolCompleted,
            JToken.FromObject(result),
            sequence: sequence);
    }

    public static ConversationEvent Completed(long sequence = 0)
    {
        return Create(
            ConversationEventType.ConversationCompleted,
            sequence: sequence);
    }

    public static ConversationEvent Cancelled(long sequence = 0)
    {
        return Create(
            ConversationEventType.ConversationCancelled,
            sequence: sequence);
    }

    public static ConversationEvent Failed(string message, long sequence = 0)
    {
        return Create(
            ConversationEventType.ConversationFailed,
            message: message,
            sequence: sequence);
    }
}