using Codify.Core.Models;
using System;
using System.Threading.Tasks;

namespace Codify.Core.UseCases;

/// <summary>
/// Handles sending a user message to an AI provider and returning the result.
/// </summary>
public interface ISendChatMessageUseCase
{
    /// <summary>
    /// Executes the chat flow for a single user message.
    /// </summary>
    Task<ChatResponse> ExecuteAsync(ChatMessage message, bool includeSelectedCode);

    /// <summary>
    /// Executes the chat request in streaming mode.
    /// The callback receives both chunk and final messages.
    /// </summary>
    Task ExecuteStreamingAsync(
        ChatMessage message,
        bool includeSelectedCode,
        Func<ChatResponse, Task> onMessage);
}