using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.UseCases;

/// <summary>
/// Handles sending a user message to an AI provider and returning the result.
/// </summary>
public interface ISendChatMessageUseCase
{
    /// <summary>
    /// Executes the chat flow for a single user message.
    /// </summary>
    Task<ChatResponse> ExecuteAsync(ChatRequest request, bool includeSelectedCode);
}