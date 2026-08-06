using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Infrastructure.Conversation
{
    [AutoDiRegister(Modules.Conversation, RegistrationOrder.Infrastructure)]
    public sealed class ConversationEngine(
        IAiProviderRouter aiProviderRouter,
        IAiToolRegistry toolRegistry,
        IJsonSerializer jsonSerializer)
        : IConversationEngine
    {
        private const int MaxAttempts = 5;
        private const string PreprocessorMetadataPrefix = "Preprocessor Metadata:";

        public async Task<string> ExecuteTextAsync(
            ChatMessageBuildResult request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var history = request.Messages.ToList();
            var provider = aiProviderRouter.GetCurrentProvider();
            var preprocessing = await PreprocessAsync(
                request,
                history,
                cancellationToken);

            if (preprocessing.IsDirectAnswer)
            {
                return preprocessing.DirectResponse ?? string.Empty;
            }

            return await provider.SendAsync(
                history,
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

            yield return ConversationEvent.Status("Sending request...");

            var history = request.Messages.ToList();
            var provider = aiProviderRouter.GetCurrentProvider();
            var preprocessing = await PreprocessAsync(
                request,
                history,
                cancellationToken);

            if (preprocessing.IsDirectAnswer)
            {
                if (!string.IsNullOrEmpty(preprocessing.DirectResponse))
                {
                    yield return ConversationEvent.TextDelta(preprocessing.DirectResponse);
                }

                yield return ConversationEvent.Completed();
                yield break;
            }

            await foreach (var evt in ProcessEvents(
                               history,
                               () => provider.SendStreamAsync(
                                   history,
                                   cancellationToken),
                               cancellationToken))
            {
                yield return evt;
            }
        }

        private async Task<PreprocessingOutcome> PreprocessAsync(
            ChatMessageBuildResult request,
            List<ChatMessage> history,
            CancellationToken cancellationToken)
        {
            var preprocessorProvider = aiProviderRouter.GetCurrentPreprocessorProvider();

            if (preprocessorProvider == null)
            {
                return PreprocessingOutcome.Forward();
            }

            var result = await preprocessorProvider.PreprocessAsync(
                history,
                cancellationToken);

            if (result == null)
            {
                return PreprocessingOutcome.Forward();
            }

            request.Context.PreprocessorResult = result;
            ApplyPreprocessorResult(history, result);

            if (result.IsAnswer)
            {
                return PreprocessingOutcome.Answer(result.Response);
            }

            if (result.IsForward)
            {
                AddPreprocessorMetadata(history, result);
            }

            return PreprocessingOutcome.Forward();
        }

        private static void ApplyPreprocessorResult(
            IReadOnlyList<ChatMessage> messages,
            AiPreprocessorResult result)
        {
            var context = messages?
                .LastOrDefault(x => string.Equals(x.Role, "user", StringComparison.OrdinalIgnoreCase) && x.Context != null)
                ?.Context;

            if (context != null)
            {
                context.PreprocessorResult = result;
            }
        }

        private static void AddPreprocessorMetadata(
            List<ChatMessage> history,
            AiPreprocessorResult result)
        {
            if (history.Any(IsPreprocessorMetadataMessage))
            {
                return;
            }

            var insertionIndex = 0;

            while (insertionIndex < history.Count && IsSystemMessage(history[insertionIndex]))
            {
                insertionIndex++;
            }

            history.Insert(
                insertionIndex,
                CreatePreprocessorMetadataMessage(result));
        }

        private static ChatMessage CreatePreprocessorMetadataMessage(AiPreprocessorResult result)
        {
            var metadata = new JObject
            {
                ["action"] = "forward",
                ["user"] = result.User,
                ["needsPlanner"] = result.NeedsPlanner,
                ["needsWorkspaceContext"] = result.NeedsWorkspaceContext,
                ["contextsNeeded"] = new JArray(result.ContextsNeeded ?? new List<string>()),
                ["toolsNeeded"] = new JArray(result.ToolsNeeded ?? new List<string>())
            };

            return new ChatMessage
            {
                Role = "system",
                Content = PreprocessorMetadataPrefix + Environment.NewLine + metadata.ToString(Formatting.None)
            };
        }

        private static bool IsPreprocessorMetadataMessage(ChatMessage message)
        {
            return IsSystemMessage(message) &&
                   message.Content?.StartsWith(PreprocessorMetadataPrefix, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsSystemMessage(ChatMessage message)
        {
            return string.Equals(message?.Role, "system", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Processes conversation events recursively.
        /// </summary>
        private async IAsyncEnumerable<ConversationEvent> ProcessEvents(
            List<ChatMessage> history,
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

                                var result = await tool.ExecuteAsync(
                                    toolRequest,
                                    cancellationToken);

                                results.Add(result);

                                history.Add(new ChatMessage
                                {
                                    Role = "tool",
                                    ToolCallId = result.Id,
                                    Content = jsonSerializer.Serialize(result.Data),
                                });

                                yield return ConversationEvent.ToolCompleted(result);
                            }

                            var provider = aiProviderRouter.GetCurrentProvider();

                            await foreach (var continuationEvent in ProcessEvents(
                                               history,
                                               () => provider.ContinueAsync(
                                                   history,
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

            foreach (var name in new[] { "path", "query", "symbol", "id", "title" })
            {
                var detail = request.Arguments.Value<string>(name);

                if (string.IsNullOrWhiteSpace(detail))
                {
                    continue;
                }

                return name == "path" ? GetPathDisplayName(detail) : detail;
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

        private sealed class PreprocessingOutcome
        {
            public bool IsDirectAnswer { get; private set; }

            public string DirectResponse { get; private set; }

            public static PreprocessingOutcome Forward()
            {
                return new PreprocessingOutcome();
            }

            public static PreprocessingOutcome Answer(string response)
            {
                return new PreprocessingOutcome
                {
                    IsDirectAnswer = true,
                    DirectResponse = response
                };
            }
        }
    }
}