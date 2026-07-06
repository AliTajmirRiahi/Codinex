using Codify.Core.Abstractions;
using Codify.Core.Chat;
using Codify.Core.Models;
using Codify.Storage;
using Codify.Storage.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AiProviders
{
    /// <summary>
    /// </summary>
    public class GapGptProvider : IAiProvider
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ProviderManager _providerManager;

        // Using a static HttpClient is a best practice to prevent socket exhaustion.
        private static readonly HttpClient HttpClient = new HttpClient();

        public GapGptProvider(IJsonSerializer jsonSerializer, ProviderManager providerManager)
        {
            _jsonSerializer = jsonSerializer;
            _providerManager = providerManager;
        }

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

            var jsonPayload = _jsonSerializer.Serialize(payload);
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

            var jsonResponse = _jsonSerializer.Parse(responseContent);
            var aiMessage = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

            return aiMessage ?? throw new HttpRequestException("No response content received.");
        }

        public async Task SendStreamAsync(
            IReadOnlyList<ChatMessage> prompts,
            Func<string, Task> onChunk,
            CancellationToken ct = default)
        {
            try
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
                    stream = true
                };

                var jsonPayload = _jsonSerializer.Serialize(payload);
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
                    ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"AI Error ({response.StatusCode}): {errorText}");
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (!line.StartsWith("data:"))
                        continue;

                    var json = line.Substring(5).Trim();

                    if (json == "[DONE]")
                        break;

                    try
                    {
                        var obj = _jsonSerializer.Parse(json);
                        var chunk = obj["choices"]?[0]?["delta"]?["content"]?.ToString();

                        if (!string.IsNullOrEmpty(chunk))
                        {
                            await Task.Delay(50, ct);
                            await onChunk(chunk);
                        }
                    }
                    catch
                    {
                        // Ignore malformed partial chunks
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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