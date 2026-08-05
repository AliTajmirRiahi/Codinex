using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.AI.Errors;
using Codinex.Infrastructure.CustomeExceptions;
using Codinex.Storage.Managers;

namespace Codinex.Infrastructure.AI.Providers
{
    /// <summary>
    /// Native Ollama provider using the local /api/chat endpoint.
    /// </summary>
    public class OllamaProvider(
        IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        IHttpService httpService)
        : IAiProvider
    {
        private readonly ProviderManager _providerManager = providerManager;

        public async Task<string> SendAsync(
            IReadOnlyList<ChatMessage> prompt,
            CancellationToken ct = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            try
            {
                var response = await PostAsync(
                    provider,
                    BuildChatPayload(model, prompt, false),
                    ct);

                var json = jsonSerializer.Parse(response);

                return json["message"]?["content"]?.ToString()
                       ?? throw new HttpRequestException("No response content received from Ollama.");
            }
            catch (Exception ex)
            {
                if (!AiErrorFactory.TryCreateExpected(ex, ct, out var error))
                {
                    throw;
                }

                return error.Message;
            }
        }

        public IAsyncEnumerable<ConversationEvent> SendStreamAsync(
            IReadOnlyList<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            return MapExpectedErrors(
                StreamChatAsync(
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
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            return MapExpectedErrors(
                StreamChatAsync(
                    provider,
                    model,
                    history,
                    cancellationToken),
                cancellationToken);
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

        private async IAsyncEnumerable<ConversationEvent> StreamChatAsync(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                provider,
                "/api/chat");

            request.Content = new StringContent(
                jsonSerializer.Serialize(BuildChatPayload(model, messages, true)),
                Encoding.UTF8,
                "application/json");

            using var response = await httpService.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();

                yield return AiErrorFactory.ToConversationEvent(
                    AiErrorFactory.FromHttpStatusCode(
                        response.StatusCode,
                        body,
                        response.Headers.RetryAfter?.Delta));
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var completed = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();

                if (line == null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var json = jsonSerializer.Parse(line);

                var content = json["message"]?["content"]?.ToString();

                if (!string.IsNullOrEmpty(content))
                {
                    yield return ConversationEvent.TextDelta(content);
                }

                if (json.Value<bool?>("done") == true)
                {
                    completed = true;
                    yield return ConversationEvent.Completed();
                    yield break;
                }
            }

            if (!completed)
            {
                yield return ConversationEvent.Completed();
            }
        }

        private async Task<string> PostAsync(
            AiProvider provider,
            object payload,
            CancellationToken cancellationToken)
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                provider,
                "/api/chat");

            request.Content = new StringContent(
                jsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await httpService.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();

            var body = await response.Content.ReadAsStringAsync();

            throw new OpenAiCompatibleException(
                response.StatusCode,
                body,
                response.Headers.RetryAfter?.Delta);
        }

        private object BuildChatPayload(
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream)
        {
            return new
            {
                model = model.Id,
                messages = BuildMessages(messages),
                stream
            };
        }

        private static List<object> BuildMessages(IReadOnlyList<ChatMessage> prompts)
        {
            var messages = new List<object>();

            foreach (var prompt in prompts)
            {
                var role = NormalizeRole(prompt.Role);
                var content = prompt.Content ?? string.Empty;

                if (prompt.Role?.Trim().Equals("tool", StringComparison.OrdinalIgnoreCase) == true)
                {
                    content = string.IsNullOrWhiteSpace(prompt.ToolCallId)
                        ? content
                        : $"Tool result ({prompt.ToolCallId}): {content}";
                }

                var images = GetImages(prompt.Data);

                if (images.Length > 0)
                {
                    messages.Add(new
                    {
                        role,
                        content,
                        images
                    });
                }
                else
                {
                    messages.Add(new
                    {
                        role,
                        content
                    });
                }
            }

            return messages;
        }

        private static string[] GetImages(JObject data)
        {
            if (data?["images"] is not JArray images || images.Count == 0)
            {
                return Array.Empty<string>();
            }

            return images
                .Select(x => StripDataUri(x["base64"]?.ToString()))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }

        private static string StripDataUri(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return string.Empty;

            var commaIndex = base64.IndexOf(',');

            return base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0
                ? base64.Substring(commaIndex + 1)
                : base64;
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            AiProvider provider,
            string endpoint)
        {
            var baseUrl = provider.BaseUrl.TrimEnd('/');

            var request = new HttpRequestMessage(
                method,
                $"{baseUrl}/{endpoint.TrimStart('/')}");

            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        provider.ApiKey);
            }

            return request;
        }

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "user";

            return role.Trim().ToLowerInvariant() switch
            {
                "assistant" => "assistant",
                "system" => "system",
                _ => "user"
            };
        }
    }
}
