using Codinex.Core.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Tools;
using Codinex.Core.Workspace.Prompt;
using Newtonsoft.Json;

namespace Codinex.Core.UseCases;

/// <summary>
/// Orchestrates the flow of sending a message to AI. 
/// Located in Core because it represents the "Business Logic" of the extension.
/// </summary>
[AutoDiRegister(Modules.Conversation, RegistrationOrder.Infrastructure)]
public sealed class SendChatMessageUseCase(
    IChatSession chatSession,
    IErrorHandler errorHandler,
    IChatMessageBuilder chatMessageBuilder,
    IConversationEngine conversationEngine,
    IWorkspaceContextBuilder workspaceContextBuilder,
    IAiProviderRouter aiProviderRouter,
    IAiToolRegistry toolRegistry)
    : ISendChatMessageUseCase
{
    public async Task<ChatResponse> ExecuteAsync(ChatMessageBuildRequest request, bool includeSelectedCode)
    {
        if (request == null)
            throw new InvalidOperationException("Request cannot be empty.");

        try
        {
            // Get last 10 messages for context
            request.ConversationHistory = chatSession.GetRecentMessages(10);

            var preprocessorResult = await RunPreprocessorAsync(
                request,
                CancellationToken.None);

            if (preprocessorResult?.IsAnswer == true)
            {
                var directResult = CreateDirectResponseResult(request, preprocessorResult);
                var directResponse = preprocessorResult.Response ?? string.Empty;

                chatSession.AddUserMessage(request.DraftText, directResult.Context);
                chatSession.AddAssistantMessage(directResponse);

                var directTitleChanged = await chatSession.SaveAsync();

                return new ChatResponse(
                    WebViewMessageType.AiResponse,
                    directResponse,
                    CreateResponseMeta(directResult, directTitleChanged));
            }

            var workspaceRequest = new WorkspaceContextRequest
            {
                Conversation = request.ConversationHistory,
                ContextsNeeded = preprocessorResult?.IsForward == true
                    ? preprocessorResult.ContextsNeeded ?? new List<string>()
                    : null
            };

            var promptContext =
                await workspaceContextBuilder.BuildAsync(
                    workspaceRequest,
                    CancellationToken.None);

            var buildResult = chatMessageBuilder.Build(request, promptContext);
            buildResult.Context.PreprocessorResult = preprocessorResult;

            var aiResult = await conversationEngine.ExecuteTextAsync(
                buildResult,
                CancellationToken.None);

            // Persist the exchange only after a successful AI response.
            // Provider errors must not be saved into message history.
            chatSession.AddUserMessage(request.DraftText, buildResult.Context);
            chatSession.AddAssistantMessage(aiResult);

            // Save session
            var titleChanged = await chatSession.SaveAsync();

            var meta = CreateResponseMeta(buildResult, titleChanged);

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

            var preprocessorResult = await RunPreprocessorStreamingAsync(
                request,
                onMessage,
                cancellationToken);

            var tt = Newtonsoft.Json.JsonConvert.SerializeObject(preprocessorResult);

            if (preprocessorResult?.IsAnswer == true)
            {
                buildResult = CreateDirectResponseResult(request, preprocessorResult);
                fullText = preprocessorResult.Response ?? string.Empty;

                chatSession.AddUserMessage(request.DraftText, buildResult.Context);
                chatSession.AddAssistantMessage(fullText);

                var directTitleChanged = await chatSession.SaveAsync();

                await onMessage(new ChatResponse(
                    WebViewMessageType.AiResponse,
                    fullText,
                    CreateResponseMeta(buildResult, directTitleChanged)));

                return;
            }

            var workspaceRequest = new WorkspaceContextRequest
            {
                Conversation = request.ConversationHistory,
                References = request.SelectedReferences,
                ContextsNeeded = preprocessorResult?.IsForward == true
                    ? preprocessorResult.ContextsNeeded ?? []
                    : null
            };

            var promptContext =
                await workspaceContextBuilder.BuildAsync(
                    workspaceRequest,
                    cancellationToken);

            buildResult = chatMessageBuilder.Build(request, promptContext);
            buildResult.Context.PreprocessorResult = preprocessorResult;

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

                        await Task.Delay(50, cancellationToken); // Small delay to avoid overwhelming the UI with too many messages.

                        await onMessage(
                            new ChatResponse(
                                WebViewMessageType.StreamChunk,
                                chunk,
                                CreateResponseMeta(buildResult)));

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

            // Emit the final completed response.
            await onMessage(new ChatResponse(
                WebViewMessageType.AiResponse,
                fullText,
                CreateResponseMeta(buildResult, titleChanged)));
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

            var meta = CreateResponseMeta(buildResult, titleChanged, true);

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

    private async Task<AiPreprocessorResult> RunPreprocessorAsync(
        ChatMessageBuildRequest request,
        CancellationToken cancellationToken)
    {
        var preprocessorProvider = aiProviderRouter.GetCurrentPreprocessorProvider();

        if (preprocessorProvider == null)
        {
            return null;
        }

        var buildResult = CreatePreprocessorBuildResult(request);
        var response = await conversationEngine.ExecuteTextAsync(
            buildResult,
            cancellationToken);

        return AiPreprocessorResultParser.ParseOrDefault(response);
    }

    private async Task<AiPreprocessorResult> RunPreprocessorStreamingAsync(
        ChatMessageBuildRequest request,
        Func<ChatResponse, Task> onMessage,
        CancellationToken cancellationToken)
    {
        var preprocessorProvider = aiProviderRouter.GetCurrentPreprocessorProvider();

        if (preprocessorProvider == null)
        {
            return null;
        }

        var preprocessorText = string.Empty;
        var buildResult = CreatePreprocessorBuildResult(request);

        var tt = Newtonsoft.Json.JsonConvert.SerializeObject(buildResult);

        await foreach (var evt in conversationEngine.ExecuteAsync(
                           buildResult,
                           cancellationToken))
        {
            switch (evt.Type)
            {
                case ConversationEventType.TextDelta:
                    await Task.Delay(50, cancellationToken);
                    preprocessorText += evt.Payload.ToString();
                    continue;

                case ConversationEventType.StatusChanged:
                    await onMessage(
                        new ChatResponse(
                            WebViewMessageType.StatusChanged,
                            evt.DisplayMessage));
                    continue;

                case ConversationEventType.ConversationFailed:
                    return null;
            }
        }

        return AiPreprocessorResultParser.ParseOrDefault(preprocessorText);
    }

    private ChatMessageBuildResult CreatePreprocessorBuildResult(ChatMessageBuildRequest request)
    {
        var messages = BuildPreprocessorMessages(request);
        var context = new ChatMessageRequestContext
        {
            SelectedCommand = request.SelectedCommand,
            SelectedAgent = request.SelectedAgent,
            SelectedReferences = request.SelectedReferences
        };

        var userMessage = messages.LastOrDefault(x =>
            string.Equals(x.Role, "user", StringComparison.OrdinalIgnoreCase));

        userMessage?.Context = context;

        return new ChatMessageBuildResult
        {
            Messages = messages,
            ProviderRole = ConversationProviderRole.Preprocessor,
            Context = context
        };
    }

    private IReadOnlyList<ChatMessage> BuildPreprocessorMessages(ChatMessageBuildRequest request)
    {
        var messages = new List<ChatMessage>
        {
            new()
            {
                Role = "system",
                Content = SystemPrompts.PreprocessorSystemPrompt
            }
        };

        if (request.ConversationHistory != null)
        {
            messages.AddRange(request.ConversationHistory.Select(CloneMessage));
        }

        messages.Add(new ChatMessage
        {
            Role = "user",
            Content = BuildPreprocessorUserContent(request)
        });

        return messages;
    }

    private string BuildPreprocessorUserContent(ChatMessageBuildRequest request)
    {
        var contexts = workspaceContextBuilder.GetAvailableContexts();
        var tools = toolRegistry
            .GetAll()
            .Where(x => x.Visibility == ToolVisibility.Model)
            .Select(x => new AiPreprocessorCatalogItem
            {
                Name = x.Name,
                Description = x.Description,
                Capabilities = x.Capabilities
            })
            .ToList();

        var sb = new StringBuilder();

        sb.AppendLine("You are the Preprocessor AI.");
        sb.AppendLine("Decide whether to answer directly or forward this request to the primary AI.");
        sb.AppendLine("Return exactly one JSON object and no markdown.");
        sb.AppendLine();
        sb.AppendLine("Direct response schema:");
        sb.AppendLine("{ \"action\": \"answer\", \"response\": \"<complete response>\" }");
        sb.AppendLine();
        sb.AppendLine("Forward schema:");
        sb.AppendLine("{ \"action\": \"forward\", \"user\": \"<original user request>\", \"needsPlanner\": false, \"needsWorkspaceContext\": false, \"contextsNeeded\": [], \"toolsNeeded\": [] }");
        sb.AppendLine();
        sb.AppendLine("Available Workspace Contexts:");
        sb.AppendLine(JsonConvert.SerializeObject(contexts, Formatting.Indented));
        sb.AppendLine();
        sb.AppendLine("Available Tools:");
        sb.AppendLine(JsonConvert.SerializeObject(tools, Formatting.Indented));
        sb.AppendLine();

        if (request.SelectedCommand is not null)
        {
            sb.AppendLine($"Command: {request.SelectedCommand.Name}");

            if (!string.IsNullOrWhiteSpace(request.SelectedCommand.Description))
            {
                sb.AppendLine($"Command Description: {request.SelectedCommand.Description}");
            }

            sb.AppendLine();
        }

        if (request.SelectedAgent is not null)
        {
            sb.AppendLine($"Agent: {request.SelectedAgent.Name}");

            if (!string.IsNullOrWhiteSpace(request.SelectedAgent.Description))
            {
                sb.AppendLine($"Agent Description: {request.SelectedAgent.Description}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("User Request:");
        sb.AppendLine(request.DraftText);

        return sb.ToString().TrimEnd();
    }

    private static ChatMessage CloneMessage(ChatMessage message)
    {
        return new ChatMessage
        {
            Role = message.Role,
            Content = message.Content,
            Data = message.Data,
            ToolCalls = message.ToolCalls,
            ToolCallId = message.ToolCallId,
            CreatedAt = message.CreatedAt,
            Stream = message.Stream
        };
    }

    private static ChatMessageBuildResult CreateDirectResponseResult(
        ChatMessageBuildRequest request,
        AiPreprocessorResult preprocessorResult)
    {
        return new ChatMessageBuildResult
        {
            ProviderRole = ConversationProviderRole.Preprocessor,
            Context = new ChatMessageRequestContext
            {
                SelectedCommand = request.SelectedCommand,
                SelectedAgent = request.SelectedAgent,
                SelectedReferences = request.SelectedReferences,
                PreprocessorResult = preprocessorResult
            }
        };
    }

    private static Dictionary<string, object> CreateResponseMeta(
        ChatMessageBuildResult buildResult,
        bool titleChanged = false,
        bool cancelled = false)
    {
        Dictionary<string, object> meta = null;

        void Add(string key, object value)
        {
            meta ??= new Dictionary<string, object>();
            meta[key] = value;
        }

        if (buildResult?.Context?.PreprocessorResult?.IsAnswer == true)
        {
            Add("isPreprocessorAnswer", true);
        }

        if (titleChanged)
        {
            Add("titleChanged", true);
        }

        if (cancelled)
        {
            Add("cancelled", true);
        }

        return meta;
    }
}