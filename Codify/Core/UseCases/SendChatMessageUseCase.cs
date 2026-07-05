using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Infrastructure.Errors;
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
    private readonly IErrorHandler _errorHandler;

    // We depend on the Interface (Abstraction), not the concrete implementation.
    // This makes it easy to swap GapGPT with Local AI or OpenAI.
    public SendChatMessageUseCase(IAiProvider aiProvider, IChatSession chatSession, IErrorHandler errorHandler)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _chatSession = chatSession;
        _errorHandler = errorHandler;
    }

    public async Task<ChatResponse> ExecuteAsync(ChatMessage message, bool includeSelectedCode)
    {
        if (message == null)
            throw new InvalidOperationException("Message cannot be empty.");

        try
        {
            // Add user message to session
            message = _chatSession.AddUserMessage(message.Content);

            // Get last 10 messages for context
            var context = _chatSession.GetRecentMessages(10);

            // Send to provider
            var aiResult = await _aiProvider.SendAsync(context);

            // Save assistant message
            _chatSession.AddAssistantMessage(aiResult);

            // Save session
            var titleChanged = await _chatSession.SaveAsync();

            if (!titleChanged) return new ChatResponse(WebViewMessageType.AiResponse, aiResult);

            var meta = new Dictionary<string, object>
            {
                ["titleChanged"] = true
            };

            return new ChatResponse(WebViewMessageType.AiResponse, aiResult, meta);
        }
        catch (Exception ex)
        {
            // Log full error details in Visual Studio Output.
            _errorHandler.Handle(ex, nameof(SendChatMessageUseCase), new
            {
                message.Content,
                includeSelectedCode
            });

            // IMPORTANT:
            // Do not save any error text into chat session.
            return new ChatResponse(WebViewMessageType.Error, _errorHandler.GetUserFacingMessage());
        }
    }

    public async Task ExecuteStreamingAsync(ChatMessage message, bool includeSelectedCode, Func<ChatResponse, Task> onMessage)
    {
        if (message == null)
            throw new InvalidOperationException("Message cannot be empty.");

        if (onMessage == null)
            throw new ArgumentNullException(nameof(onMessage));

        try
        {
            // Add user message to the session.
            message = _chatSession.AddUserMessage(message.Content);

            // Build context for the provider.
            var context = _chatSession.GetRecentMessages(10);

            // Accumulate the full assistant text while chunks arrive.
            var fullText = string.Empty;

            await _aiProvider.SendStreamAsync(
                context,
                async chunk =>
                {
                    if (string.IsNullOrEmpty(chunk))
                        return;

                    fullText += chunk;

                    // Emit a chunk to the caller/UI.
                    await onMessage(new ChatResponse(
                        WebViewMessageType.StreamChunk,
                        chunk));
                });

            // Persist the final assistant answer.
            _chatSession.AddAssistantMessage(fullText);

            await _chatSession.SaveAsync();

            // Save session
            var titleChanged = await _chatSession.SaveAsync();

            if (!titleChanged)
            {
                // Emit the final completed response.
                await onMessage(new ChatResponse(
                    WebViewMessageType.AiResponse,
                    fullText));
                return;
            }

            var meta = new Dictionary<string, object>
            {
                ["titleChanged"] = true
            };

            // Emit the final completed response.
            await onMessage(new ChatResponse(
                WebViewMessageType.AiResponse,
                fullText, meta));
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, nameof(SendChatMessageUseCase), new
            {
                message.Content,
                includeSelectedCode,
                Stream = true
            });

            await onMessage(new ChatResponse(
                WebViewMessageType.Error,
                _errorHandler.GetUserFacingMessage()));
        }
    }
}