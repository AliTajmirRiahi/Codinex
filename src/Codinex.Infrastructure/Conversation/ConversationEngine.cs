using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Chat;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.AI;
using Codinex.Core.Models.Chat;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Codinex.Infrastructure.Conversation
{
    [AutoDiRegister(Modules.Conversation, RegistrationOrder.Infrastructure)]
    public sealed class ConversationEngine(
        IAiProviderRouter aiProviderRouter,
        IAiToolRegistry toolRegistry,
        IJsonSerializer jsonSerializer,
        IToolHistoryCompactor toolHistoryCompactor)
        : IConversationEngine
    {
        private const int MaxAttempts = 5;

        /// <summary>
        /// Hard cap on a single tool result before it enters the conversation history.
        /// A pathological result (e.g. a search that matched a minified bundle or source map)
        /// would otherwise be replayed on every subsequent request for the rest of the turn.
        /// Provider-agnostic backstop that protects against every tool, not just search.
        /// </summary>
        private const int MaxToolResultLength = 25_000;

        public async Task<string> ExecuteTextAsync(
            ChatMessageBuildResult request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var history = request.Messages.ToList();
            var provider = ResolveProvider(request.ProviderRole);

            return await provider.SendAsync(
                toolHistoryCompactor.Compact(history),
                request.ChatId,
                request.ChatMessageId,
                cancellationToken);
        }

        public async IAsyncEnumerable<ConversationEvent> ExecuteAsync(
            ChatMessageBuildResult request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var status = "Sending request...";

            if (request.ProviderRole == ConversationProviderRole.Preprocessor)
                status = "Preprocessoring...";

            yield return ConversationEvent.Status(status);

            var history = request.Messages.ToList();
            var provider = ResolveProvider(request.ProviderRole);

            await foreach (var evt in ProcessEvents(
                               history,
                               request.ProviderRole,
                               request.ChatId,
                               request.ChatMessageId,
                               () => provider.SendStreamAsync(
                                   toolHistoryCompactor.Compact(history),
                                   request.ChatId,
                                   request.ChatMessageId,
                                   cancellationToken),
                               cancellationToken))
            {
                yield return evt;
            }
        }

        /// <summary>
        /// Processes conversation events recursively.
        /// </summary>
        private async IAsyncEnumerable<ConversationEvent> ProcessEvents(
            List<ChatMessage> history,
            ConversationProviderRole providerRole,
            string chatId,
            string chatMessageId,
            Func<IAsyncEnumerable<ConversationEvent>> createEvents,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ConversationEvent originalFailureEvent = null;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                ConversationEvent retryFailureEvent = null;

                await foreach (var evt in createEvents().WithCancellation(cancellationToken))
                {
                    if (evt.Type == ConversationEventType.ConversationFailed
                        && TryGetAiError(evt, out var error)
                        && ShouldRetry(error))
                    {
                        originalFailureEvent ??= evt;

                        if (attempt >= MaxAttempts)
                        {
                            yield return originalFailureEvent;
                            yield break;
                        }

                        retryFailureEvent = evt;
                        break;
                    }

                    switch (evt.Type)
                    {
                        case ConversationEventType.ToolRequested:

                            var payload = evt.Payload.ToObject<ToolRequestedPayload>();

                            var assistantMessage = payload.AssistantMessage;

                            history.Add(assistantMessage);

                            var results = new List<ToolResult>();

                            foreach (var toolRequest in payload.Requests)
                            {
                                var tool = toolRegistry.Get(toolRequest.Name);

                                var statusMessage = GetStatusMessage(tool, toolRequest);

                                if (!string.IsNullOrWhiteSpace(statusMessage))
                                {
                                    yield return ConversationEvent.Status(statusMessage);
                                }

                                ToolResult result;

                                try
                                {
                                    result = await tool.ExecuteAsync(
                                        toolRequest,
                                        cancellationToken);
                                }
                                catch (ToolRequestValidationException ex)
                                {
                                    result = ToolResult.Failed(
                                        toolRequest.Id,
                                        ex.Message,
                                        new
                                        {
                                            errorType = "missing_required_argument",
                                            toolName = ex.ToolName,
                                            argumentName = ex.ArgumentName,
                                            message = ex.Message
                                        });
                                }

                                results.Add(result);

                                history.Add(new ChatMessage
                                {
                                    Role = "tool",
                                    ToolCallId = result.Id,
                                    Content = TruncateToolContent(
                                        jsonSerializer.Serialize(new
                                        {
                                            success = result.Success,
                                            error = result.Error,
                                            data = result.Data
                                        }))
                                });

                                yield return ConversationEvent.ToolCompleted(result);
                            }

                            var provider = ResolveProvider(providerRole);

                            await foreach (var continuationEvent in ProcessEvents(
                                               history,
                                               providerRole,
                                               chatId,
                                               chatMessageId,
                                               () => provider.ContinueAsync(
                                                   toolHistoryCompactor.Compact(history),
                                                   chatId,
                                                   chatMessageId,
                                                   cancellationToken),
                                               cancellationToken))
                            {
                                yield return continuationEvent;
                            }

                            break;

                        case ConversationEventType.Unknown:
                        case ConversationEventType.TextDelta:
                        case ConversationEventType.ThinkingStarted:
                        case ConversationEventType.ThinkingUpdated:
                        case ConversationEventType.ThinkingCompleted:
                        case ConversationEventType.ToolCompleted:
                        case ConversationEventType.ConversationCompleted:
                        case ConversationEventType.ConversationCancelled:
                        case ConversationEventType.ConversationFailed:
                        case ConversationEventType.StatusChanged:
                        default:

                            yield return evt;

                            break;
                    }
                }

                if (retryFailureEvent == null)
                {
                    yield break;
                }

                var retryAttempt = attempt;

                yield return ConversationEvent.Status($"Retrying connection ({retryAttempt}/{MaxAttempts})...");

                var retryDelay = GetRetryDelay(
                    retryFailureEvent.Payload.ToObject<AiError>(),
                    retryAttempt);

                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        private IAiProvider ResolveProvider(ConversationProviderRole providerRole)
        {
            if (providerRole == ConversationProviderRole.Preprocessor)
            {
                return aiProviderRouter.GetCurrentPreprocessorProvider()
                       ?? throw new InvalidOperationException("Preprocessor Provider not found.");
            }

            return aiProviderRouter.GetCurrentProvider();
        }

        private static string GetStatusMessage(
            IAiTool tool,
            ToolRequest request)
        {
            var statusMessage = tool.StatusMessage;

            if (string.IsNullOrWhiteSpace(statusMessage))
            {
                return statusMessage;
            }

            var detail = GetStatusDetail(request);

            return string.IsNullOrWhiteSpace(detail) ? statusMessage : $"{statusMessage} ({detail})";
        }

        private static string GetStatusDetail(ToolRequest request)
        {
            if (request.Arguments is not { HasValues: true })
            {
                return string.Empty;
            }

            foreach (var name in new[] { "path", "filePath", "query", "symbol", "id", "title", "elementId" })
            {
                var detail = request.Arguments.Value<string>(name);

                if (string.IsNullOrWhiteSpace(detail))
                {
                    continue;
                }

                if (name.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return GetPathDisplayName(detail);
                }

                if (name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return GetIdDisplayName(detail);
                }

                return detail;
            }

            return string.Empty;
        }

        private static bool TryGetAiError(
            ConversationEvent evt,
            out AiError error)
        {
            error = null;

            if (evt.Payload == null)
            {
                return false;
            }

            try
            {
                error = evt.Payload.ToObject<AiError>();

                return error != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool ShouldRetry(AiError error)
        {
            if (error is not { IsRetryable: true })
            {
                return false;
            }

            return error.Code switch
            {
                AiErrorCode.Network or AiErrorCode.Timeout or AiErrorCode.ProviderUnavailable
                    or AiErrorCode.RateLimitExceeded => true,
                _ => false
            };
        }

        private static TimeSpan GetRetryDelay(
            AiError error,
            int retryAttempt)
        {
            if (error?.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero)
            {
                return retryAfter;
            }

            return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
        }

        private static string TruncateToolContent(string content)
        {
            if (string.IsNullOrEmpty(content) || content.Length <= MaxToolResultLength)
            {
                return content;
            }

            var omitted = content.Length - MaxToolResultLength;

            return content.Substring(0, MaxToolResultLength) +
                   $"\n\n[tool result truncated to save context: {omitted:N0} of {content.Length:N0} chars omitted. " +
                   "Narrow the request (a more specific query, a single file, or one element) to retrieve the rest.]";
        }

        private static string GetPathDisplayName(string path)
        {
            var normalizedPath = path
                .Replace('\\', '/')
                .TrimEnd('/');

            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return string.Empty;
            }

            var lastSeparatorIndex = normalizedPath.LastIndexOf('/');

            return lastSeparatorIndex >= 0
                ? normalizedPath.Substring(lastSeparatorIndex + 1)
                : normalizedPath;
        }

        private static string GetIdDisplayName(string id)
        {
            var trimmedId = id.Trim();

            if (string.IsNullOrWhiteSpace(trimmedId))
            {
                return string.Empty;
            }

            var lastSeparatorIndex = trimmedId.LastIndexOf('.');

            return lastSeparatorIndex >= 0
                ? trimmedId.Substring(lastSeparatorIndex + 1)
                : trimmedId;
        }

    }
}