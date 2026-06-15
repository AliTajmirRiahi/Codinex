using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Infrastructure.ChatSessions;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codify.Core.UseCases;

/// <summary>
/// Orchestrates the flow of sending a message to AI. 
/// Located in Core because it represents the "Business Logic" of the extension.
/// </summary>
public sealed class SendChatMessageUseCase : ISendChatMessageUseCase
{
    private readonly IAiProvider _aiProvider;
    private readonly IChatSession _chatSession;

    // We depend on the Interface (Abstraction), not the concrete implementation.
    // This makes it easy to swap GapGPT with Local AI or OpenAI.
    public SendChatMessageUseCase(IAiProvider aiProvider, IChatSession chatSession)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _chatSession = chatSession;
    }

    public async Task<ChatResponse> ExecuteAsync(ChatMessage message, bool includeSelectedCode)
    {
        if (message == null)
        {
            return new ChatResponse("ERROR", "Message cannot be empty.");
        }

        try
        {
            // Add user message to session
            _chatSession.AddUserMessage(message.Content);

            // Get last 10 messages for context
            var context = _chatSession.GetRecentMessages(10);

            // Send to provider
            var aiResult = await _aiProvider.SendAsync(context);

            // Save assistant message
            _chatSession.AddAssistantMessage(aiResult);

            // Save session
            await _chatSession.SaveAsync();

            return new ChatResponse("AI_RESPONSE", aiResult);
        }
        catch (Exception ex)
        {
            return new ChatResponse("ERROR", $"AI Provider Error: {ex.Message}");
        }
    }
}