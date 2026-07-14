using Codify.Core.Interfaces;
using Codify.Core.Chat;
using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codify.Core.Conversation;

namespace Codify.Core.UseCases;

/// <summary>
/// Orchestrates the flow of sending a message to AI. 
/// Located in Core because it represents the "Business Logic" of the extension.
/// </summary>
public sealed class SendChatMessageUseCase(
    IAiProvider aiProvider,
    IChatSession chatSession,
    IErrorHandler errorHandler,
    IChatMessageBuilder chatMessageBuilder,
    IConversationEngine conversationEngine)
    : ISendChatMessageUseCase
{
    private readonly IAiProvider _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));

    // We depend on the Interface (Abstraction), not the concrete implementation.
    // This makes it easy to swap GapGPT with Local AI or OpenAI.

    public async Task<ChatResponse> ExecuteAsync(ChatMessageBuildRequest request, bool includeSelectedCode)
    {
        if (request == null)
            throw new InvalidOperationException("Request cannot be empty.");

        try
        {
            // Get last 10 messages for context
            request.ConversationHistory = chatSession.GetRecentMessages(10);

            var buildResult = chatMessageBuilder.Build(request);

            // Add user message to session
            chatSession.AddUserMessage(request.DraftText, buildResult.Context);


            // Send to provider
            var aiResult = await _aiProvider.SendAsync(buildResult.Messages);

            // Save assistant message
            chatSession.AddAssistantMessage(aiResult);

            // Save session
            var titleChanged = await chatSession.SaveAsync();

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
            errorHandler.Handle(ex, nameof(SendChatMessageUseCase), new
            {
                request,
                includeSelectedCode
            });

            // IMPORTANT:
            // Do not save any error text into chat session.
            return new ChatResponse(WebViewMessageType.Error, errorHandler.GetUserFacingMessage());
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
            // Get last 10 messages for context
            request.ConversationHistory = chatSession.GetRecentMessages(10);

            var buildResult = chatMessageBuilder.Build(request);

            // Add user message to session
            chatSession.AddUserMessage(request.DraftText, buildResult.Context);

            // Accumulate the full assistant text while chunks arrive.
            var fullText = string.Empty;

            await foreach (var evt in conversationEngine.ExecuteAsync(
                               buildResult))
            {
                switch (evt.Type)
                {
                    case ConversationEventType.TextDelta:

                        var chunk = evt.Payload.ToString();

                        fullText += chunk;

                        await onMessage(
                            new ChatResponse(
                                WebViewMessageType.StreamChunk,
                                chunk));

                        break;

                    case ConversationEventType.StatusChanged:

                        //await onMessage(
                        //    new ChatResponse(
                        //        WebViewMessageType.StatusChanged,
                        //        evt.DisplayMessage));

                        break;
                }
            }

            // Persist the final assistant answer.
            chatSession.AddAssistantMessage(fullText);

            await chatSession.SaveAsync();

            // Save session
            var titleChanged = await chatSession.SaveAsync();

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
            errorHandler.Handle(ex, nameof(SendChatMessageUseCase), new
            {
                request,
                includeSelectedCode,
                Stream = true
            });

            await onMessage(new ChatResponse(
                WebViewMessageType.Error,
                errorHandler.GetUserFacingMessage()));
        }
    }
}