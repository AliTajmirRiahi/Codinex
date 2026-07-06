using Codify.Core.Abstractions;
using Codify.Core.Chat;
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
    private readonly IChatMessageBuilder _chatMessageBuilder;

    // We depend on the Interface (Abstraction), not the concrete implementation.
    // This makes it easy to swap GapGPT with Local AI or OpenAI.
    public SendChatMessageUseCase(
        IAiProvider aiProvider,
        IChatSession chatSession,
        IErrorHandler errorHandler,
        IChatMessageBuilder chatMessageBuilder)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _chatSession = chatSession;
        _errorHandler = errorHandler;
        _chatMessageBuilder = chatMessageBuilder;
    }

    public async Task<ChatResponse> ExecuteAsync(ChatMessageBuildRequest request, bool includeSelectedCode)
    {
        if (request == null)
            throw new InvalidOperationException("Request cannot be empty.");

        try
        {
            // Add user message to session
            _chatSession.AddUserMessage(request.DraftText);

            // Get last 10 messages for context
            request.ConversationHistory = _chatSession.GetRecentMessages(10);

            var buildResult = _chatMessageBuilder.Build(request);

            // Send to provider
            var aiResult = await _aiProvider.SendAsync(buildResult.Messages);

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
                request,
                includeSelectedCode
            });

            // IMPORTANT:
            // Do not save any error text into chat session.
            return new ChatResponse(WebViewMessageType.Error, _errorHandler.GetUserFacingMessage());
        }
    }

    public async Task ExecuteStreamingAsync(ChatMessageBuildRequest request, bool includeSelectedCode, Func<ChatResponse, Task> onMessage)
    {
        if (request == null)
            throw new InvalidOperationException("Request cannot be empty.");

        if (onMessage == null)
            throw new ArgumentNullException(nameof(onMessage));

        try
        {
            // Add user message to session
            _chatSession.AddUserMessage(request.DraftText);

            // Get last 10 messages for context
            request.ConversationHistory = _chatSession.GetRecentMessages(10);

            var buildResult = _chatMessageBuilder.Build(request);

            // Accumulate the full assistant text while chunks arrive.
            var fullText = string.Empty;

            await _aiProvider.SendStreamAsync(
                buildResult.Messages,
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
                request,
                includeSelectedCode,
                Stream = true
            });

            await onMessage(new ChatResponse(
                WebViewMessageType.Error,
                _errorHandler.GetUserFacingMessage()));
        }
    }
}