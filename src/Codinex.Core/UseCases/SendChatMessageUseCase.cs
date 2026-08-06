using Codinex.Core.Chat;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Workspace.Prompt;

namespace Codinex.Core.UseCases;

/// <summary>
/// Orchestrates the flow of sending a message to AI. 
/// Located in Core because it represents the "Business Logic" of the extension.
/// </summary>
[AutoDiRegister(Modules.Conversation, RegistrationOrder.Infrastructure)]
public sealed class SendChatMessageUseCase(
    IAiProvider aiProvider,
    IChatSession chatSession,
    IErrorHandler errorHandler,
    IChatMessageBuilder chatMessageBuilder,
    IConversationEngine conversationEngine,
    IWorkspaceContextBuilder workspaceContextBuilder)
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

            var workspaceRequest = new WorkspaceContextRequest
            {
                Conversation = request.ConversationHistory
            };

            var promptContext =
                await workspaceContextBuilder.BuildAsync(
                    workspaceRequest,
                    CancellationToken.None);

            var buildResult = chatMessageBuilder.Build(request, promptContext);

            // Send to provider
            var aiResult = await _aiProvider.SendAsync(buildResult.Messages);

            // Persist the exchange only after a successful AI response.
            // Provider errors must not be saved into message history.
            chatSession.AddUserMessage(request.DraftText, buildResult.Context);
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

    public async Task ExecuteStreamingAsync(
        ChatMessageBuildRequest request,
        bool includeSelectedCode,
        Func<ChatResponse, Task> onMessage,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new InvalidOperationException("Request cannot be empty.");

        if (onMessage == null)
            throw new ArgumentNullException(nameof(onMessage));

        var fullText = string.Empty;

        ChatMessageBuildResult buildResult = null;

        try
        {
            // Get last 10 messages for context
            request.ConversationHistory = chatSession.GetRecentMessages(10);

            var workspaceRequest = new WorkspaceContextRequest
            {
                Conversation = request.ConversationHistory,
                References = request.SelectedReferences,
            };

            var promptContext =
                await workspaceContextBuilder.BuildAsync(
                    workspaceRequest,
                    cancellationToken);

            buildResult = chatMessageBuilder.Build(request, promptContext);

            // Accumulate the full assistant text while chunks arrive.
            await foreach (var evt in conversationEngine.ExecuteAsync(
                               buildResult,
                               cancellationToken))
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

                        continue;

                    case ConversationEventType.StatusChanged:

                        await onMessage(
                            new ChatResponse(
                                WebViewMessageType.StatusChanged,
                                evt.DisplayMessage));

                        continue;

                    case ConversationEventType.ConversationFailed:

                        await onMessage(
                            new ChatResponse(
                                WebViewMessageType.Error,
                                evt.DisplayMessage));

                        return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Persist the exchange only after a successful AI response.
            // Provider errors must not be saved into message history.
            chatSession.AddUserMessage(request.DraftText, buildResult.Context);
            chatSession.AddAssistantMessage(fullText);

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
        catch (OperationCanceledException)
        {
            var titleChanged = false;

            if (!string.IsNullOrWhiteSpace(fullText) && buildResult != null)
            {
                chatSession.AddUserMessage(request.DraftText, buildResult.Context);
                chatSession.AddAssistantMessage(fullText);
                titleChanged = await chatSession.SaveAsync();
            }

            var meta = new Dictionary<string, object>
            {
                ["cancelled"] = true
            };

            if (titleChanged)
            {
                meta["titleChanged"] = true;
            }

            await onMessage(new ChatResponse(
                WebViewMessageType.AiResponse,
                fullText,
                meta));
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