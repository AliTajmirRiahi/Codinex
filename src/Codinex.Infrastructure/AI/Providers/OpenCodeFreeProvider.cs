using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.AI.Errors;
using Codinex.Infrastructure.AI.Providers.OpenCode;
using Codinex.Infrastructure.Chat;
using Codinex.Infrastructure.CustomeExceptions;
using Codinex.Storage.Managers;
using System.Text;

namespace Codinex.Infrastructure.AI.Providers
{
    /// <summary>
    /// Talks exclusively to the OpenCode Server API (https://opencode.ai/docs/server/) to access
    /// OpenCode's free models. Unlike <see cref="OpenAiCompatibleProvider"/>, OpenCode is not treated
    /// as an OpenAI-compatible chat/completions endpoint: it has its own session model and its own
    /// "GET /event" server-sent-events bus that carries assistant text as it is generated.
    ///
    /// Responsibilities:
    ///  - GetModelsAsync is handled separately by <see cref="Codinex.Infrastructure.ModelManagement.Retrievers.OpenCodeFreeModelRetriever"/>.
    ///  - CreateSessionAsync / SendMessageAsync / StreamEventsAsync are implemented here.
    /// </summary>
    public class OpenCodeFreeProvider(
        IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        IProviderClient client,
        IOpenCodeSessionStore sessionStore,
        ChatSessionService chatSessionService)
        : IAiProvider
    {
        private const string UpstreamProviderId = "opencode";

        private const string EventsEndpoint = "/event";

        private const string SessionEndpoint = "/session";

        public async Task<string> SendAsync(
            IReadOnlyList<ChatMessage> prompt,
            CancellationToken ct = default)
        {
            var sb = new StringBuilder();

            await foreach (var evt in SendStreamAsync(prompt, ct))
            {
                switch (evt.Type)
                {
                    case ConversationEventType.TextDelta:
                        sb.Append(evt.Payload?.ToString());
                        break;

                    case ConversationEventType.ConversationFailed:
                        return evt.DisplayMessage ?? "The OpenCode server returned an error.";
                }
            }

            return sb.ToString();
        }

        public IAsyncEnumerable<ConversationEvent> SendStreamAsync(
            IReadOnlyList<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            var provider = providerManager.ActiveProvider;
            var model = providerManager.ActiveModel;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            return MapExpectedErrors(
                StreamConversationAsync(
                    provider,
                    model,
                    messages,
                    cancellationToken),
                cancellationToken);
        }

        public IAsyncEnumerable<ConversationEvent> ContinueAsync(
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            // OpenCode does not participate in Codinex's local tool-calling loop (it manages its
            // own tools server-side), so ContinueAsync is never actually driven by a prior
            // ToolRequested event for this provider. It is implemented the same way as
            // SendStreamAsync for interface completeness.
            return SendStreamAsync(history, cancellationToken);
        }

        private static async IAsyncEnumerable<ConversationEvent> MapExpectedErrors(
            IAsyncEnumerable<ConversationEvent> events,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var enumerator = events.GetAsyncEnumerator(cancellationToken);

            try
            {
                while (true)
                {
                    ConversationEvent current = null;
                    AiError error = null;
                    var hasCurrent = false;

                    try
                    {
                        hasCurrent = await enumerator.MoveNextAsync();

                        if (hasCurrent)
                        {
                            current = enumerator.Current;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!AiErrorFactory.TryCreateExpected(ex, cancellationToken, out error))
                        {
                            throw;
                        }
                    }

                    if (error != null)
                    {
                        yield return AiErrorFactory.ToConversationEvent(error);
                        yield break;
                    }

                    if (!hasCurrent)
                    {
                        yield break;
                    }

                    yield return current;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        private async IAsyncEnumerable<ConversationEvent> StreamConversationAsync(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var text = GetLatestUserText(messages);
            var conversationId = chatSessionService.ActiveSession?.SessionId;
            var sessionId = await GetOrCreateSessionIdAsync(provider, conversationId, cancellationToken);

            var enumerator = client.StreamGetAsync(provider, EventsEndpoint, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            try
            {
                // Open the SSE connection before sending the message, so no early events are missed.
                var streamOpened = await enumerator.MoveNextAsync();

                if (!streamOpened)
                {
                    throw new OpenAiCompatibleException(
                        HttpStatusCode.BadGateway,
                        "The OpenCode event stream closed before it connected.");
                }

                sessionId = await SendMessageWithRetryAsync(
                    provider,
                    conversationId,
                    sessionId,
                    model,
                    text,
                    cancellationToken);

                // The first event (normally "server.connected") was already consumed above to
                // confirm the stream opened. It still needs to go through the same processing as
                // every other event below, in case it ever carries real content, so this loops as
                // a do/while over the event already in hand rather than fetching a new one first.
                do
                {
                    OpenCodeEventDto evt;

                    try
                    {
                        evt = jsonSerializer.Deserialize<OpenCodeEventDto>(enumerator.Current);
                    }
                    catch
                    {
                        // Malformed SSE event: ignore and keep listening.
                        continue;
                    }

                    if (string.IsNullOrEmpty(evt?.Type))
                        continue;

                    var eventSessionId = evt.Properties?.SessionID;

                    // The event bus is global; only react to events for our own session.
                    if (!string.IsNullOrEmpty(eventSessionId) &&
                        !string.Equals(eventSessionId, sessionId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    switch (evt.Type)
                    {
                        case "message.part.updated":

                            if (string.Equals(evt.Properties?.Part?.Type, "text", StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrEmpty(evt.Properties?.Delta))
                            {
                                yield return ConversationEvent.TextDelta(evt.Properties.Delta);
                            }

                            continue;

                        case "message.updated":

                            if (evt.Properties?.Info?.Error != null)
                            {
                                yield return AiErrorFactory.ToConversationEvent(
                                    MapOpenCodeError(evt.Properties.Info.Error));

                                yield break;
                            }

                            continue;

                        case "session.error":

                            yield return AiErrorFactory.ToConversationEvent(
                                MapOpenCodeError(evt.Properties?.Error)
                                ?? new AiError(AiErrorCode.Unknown, "The OpenCode server reported an error.", true));

                            yield break;

                        case "session.idle":

                            yield return ConversationEvent.Completed();

                            yield break;

                        default:
                            continue;
                    }
                } while (await enumerator.MoveNextAsync());

                // The stream closed without ever reporting completion or an error.
                throw new OpenAiCompatibleException(
                    HttpStatusCode.BadGateway,
                    "The OpenCode event stream ended unexpectedly.");
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        private async Task<string> GetOrCreateSessionIdAsync(
            AiProvider provider,
            string conversationId,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(conversationId) &&
                sessionStore.TryGetSessionId(conversationId, out var existingSessionId))
            {
                return existingSessionId;
            }

            var sessionId = await CreateSessionAsync(provider, cancellationToken);

            if (!string.IsNullOrWhiteSpace(conversationId))
                sessionStore.SetSessionId(conversationId, sessionId);

            return sessionId;
        }

        private async Task<string> CreateSessionAsync(
            AiProvider provider,
            CancellationToken cancellationToken)
        {
            var response = await client.PostAsync(
                provider,
                SessionEndpoint,
                new { },
                cancellationToken);

            var session = jsonSerializer.Deserialize<OpenCodeSessionDto>(response);

            if (string.IsNullOrWhiteSpace(session?.Id))
            {
                throw new OpenAiCompatibleException(
                    HttpStatusCode.BadGateway,
                    "The OpenCode server did not return a session id.");
            }

            return session.Id;
        }

        /// <summary>
        /// Sends the message, transparently recreating the session and retrying once if the
        /// cached OpenCode session id is no longer valid on the server (e.g. it restarted).
        /// </summary>
        private async Task<string> SendMessageWithRetryAsync(
            AiProvider provider,
            string conversationId,
            string sessionId,
            AiModel model,
            string text,
            CancellationToken cancellationToken)
        {
            try
            {
                await SendMessageAsync(provider, sessionId, model, text, cancellationToken);

                return sessionId;
            }
            catch (OpenAiCompatibleException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var freshSessionId = await CreateSessionAsync(provider, cancellationToken);

                if (!string.IsNullOrWhiteSpace(conversationId))
                    sessionStore.SetSessionId(conversationId, freshSessionId);

                await SendMessageAsync(provider, freshSessionId, model, text, cancellationToken);

                return freshSessionId;
            }
        }

        private Task<string> SendMessageAsync(
            AiProvider provider,
            string sessionId,
            AiModel model,
            string text,
            CancellationToken cancellationToken)
        {
            var payload = new
            {
                model = new
                {
                    providerID = UpstreamProviderId,
                    modelID = model.Id
                },
                parts = new object[]
                {
                    new
                    {
                        type = "text",
                        text
                    }
                }
            };

            return client.PostAsync(
                provider,
                $"{SessionEndpoint}/{sessionId}/message",
                payload,
                cancellationToken);
        }

        private static string GetLatestUserText(IReadOnlyList<ChatMessage> messages)
        {
            var last = messages?.LastOrDefault(m =>
                           string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))
                       ?? messages?.LastOrDefault();

            return last?.Content ?? string.Empty;
        }

        private static AiError MapOpenCodeError(OpenCodeErrorDto error)
        {
            if (error == null)
                return null;

            var message = error.Data?.Message;

            switch (error.Name)
            {
                case "ProviderAuthError":
                    return new AiError(
                        AiErrorCode.AuthenticationFailed,
                        message ?? "OpenCode provider authentication failed.",
                        false);

                case "MessageOutputLengthError":
                    return new AiError(
                        AiErrorCode.ContextLengthExceeded,
                        message ?? "The response exceeded the model output limit.",
                        false);

                case "MessageAbortedError":
                    return new AiError(
                        AiErrorCode.Cancelled,
                        message ?? "The OpenCode request was aborted.",
                        false);

                case "APIError" when error.Data?.StatusCode is { } statusCode:
                    return AiErrorFactory.FromHttpStatusCode((HttpStatusCode)statusCode, message);

                default:
                    return new AiError(
                        AiErrorCode.Unknown,
                        message ?? "The OpenCode server reported an error.",
                        error.Data?.IsRetryable ?? true);
            }
        }
    }
}
