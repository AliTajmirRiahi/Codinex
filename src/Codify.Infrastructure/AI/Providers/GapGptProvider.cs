using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AI.Providers
{
    /// <summary>
    /// </summary>
    public class GapGptProvider(IJsonSerializer jsonSerializer, ProviderManager providerManager)
        : IAiProvider
    {
        private readonly ProviderManager _providerManager = providerManager;

        // Using a static HttpClient is a best practice to prevent socket exhaustion.
        private static readonly HttpClient HttpClient = new HttpClient();

        public async Task<string> SendAsync(IReadOnlyList<ChatMessage> prompts, CancellationToken ct = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            var baseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
                ? "https://api.gapgpt.app/v1"
                : provider.BaseUrl.TrimEnd('/');

            var requestUri = $"{baseUrl}/chat/completions";

            var payload = new
            {
                model = model.Id,
                messages = BuildMessages(prompts),
                stream = false
            };

            var jsonPayload = jsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;

            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
            }

            var response = await HttpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"AI Error ({response.StatusCode}): {responseContent}");

            var jsonResponse = jsonSerializer.Parse(responseContent);
            var aiMessage = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

            return aiMessage ?? throw new HttpRequestException("No response content received.");
        }

        public async IAsyncEnumerable<ConversationEvent> SendStreamAsync(
            IReadOnlyList<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            var baseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
                ? "https://api.gapgpt.app/v1"
                : provider.BaseUrl.TrimEnd('/');

            var requestUri = $"{baseUrl}/chat/completions";

            var payload = new
            {
                model = model.Id,
                messages = BuildMessages(messages),
                stream = true
            };

            var jsonPayload = jsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;

            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", provider.ApiKey);
            }

            using var response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"AI Error ({response.StatusCode}): {errorText}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith("data:"))
                    continue;

                var json = line.Substring(5).Trim();

                if (json == "[DONE]")
                    break;

                var obj = jsonSerializer.Parse(json);
                var chunk = obj["choices"]?[0]?["delta"]?["content"]?.ToString();

                if (string.IsNullOrEmpty(chunk)) continue;

                await Task.Delay(50, cancellationToken);

                yield return ConversationEvent.TextDelta(chunk);
            }
        }
        public async IAsyncEnumerable<ConversationEvent> ContinueAsync(
            IReadOnlyList<ToolResult> toolResults,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var toolResult in toolResults)
            {
                yield return ConversationEvent.Status(
                    $"Tool '{toolResult.Id}' completed.");
            }

            yield return ConversationEvent.TextDelta(
                "Conversation continuation is not implemented yet.");

            yield return ConversationEvent.Completed();
        }
        private List<object> BuildMessages(IReadOnlyList<ChatMessage> prompts)
        {
            var messages = new List<object>();

            foreach (var prompt in prompts)
            {
                messages.Add(new
                {
                    role = NormalizeRole(prompt.Role),
                    content = prompt.Content
                });
            }

            return messages;
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