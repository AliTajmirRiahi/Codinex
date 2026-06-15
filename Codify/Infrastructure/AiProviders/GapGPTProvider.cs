using Codify.Core.Abstractions;
using Codify.Core.Models;
using Codify.Storage.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codify.Storage;

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

        public async Task<string> SendAsync(IReadOnlyList<ChatMessage> prompts, IEnumerable<Attachment> attachments = null, CancellationToken ct = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            // 1. Prepare the Endpoint URL
            var baseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
                ? "https://api.openai.com/v1"
                : provider.BaseUrl.TrimEnd('/');

            var requestUri = $"{baseUrl}/chat/completions";

            // 2. Prepare the Payload (Exact format you requested)
            var payload = new
            {
                model = model.Id, // e.g., "gpt-5.3-chat-latest"
                messages = BuildMessages(prompts, attachments),
                stream = false
            };

            var jsonPayload = _jsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 3. Create the Request
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;

            // Add Bearer Token if ApiKey exists
            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
            }

            try
            {
                // 4. Send the request
                var response = await HttpClient.SendAsync(request, ct);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"AI Error ({response.StatusCode}): {responseContent}";
                }

                // 5. Parse the standard OpenAI Response
                var jsonResponse = _jsonSerializer.Parse(responseContent);
                var aiMessage = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

                return aiMessage ?? "No response content received.";
            }
            catch (Exception ex)
            {
                return $"Network Error: {ex.Message}";
            }
        }

        private List<object> BuildMessages(IReadOnlyList<ChatMessage> prompts, IEnumerable<Attachment> attachments)
        {
            var messages = new List<object>();

            // Handle multi-modal content (Text + Attachments)
            var contentList = new List<object>();

            // Add the main user text
            foreach (var prompt in prompts)
                contentList.Add(new { type = "text", role = prompt.Role, text = prompt.Content });

            // Add attachments if any
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment.Type == AttachmentType.Image && attachment.RawData != null)
                    {
                        var base64Image = Convert.ToBase64String(attachment.RawData);
                        contentList.Add(new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/png;base64,{base64Image}" }
                        });
                    }
                    else if (!string.IsNullOrEmpty(attachment.Content))
                    {
                        // Add files or code snippets as text context
                        contentList.Add(new
                        {
                            type = "text",
                            text = $"\n--- {attachment.Type} ({attachment.FileName ?? "unnamed"}) ---\n{attachment.Content}"
                        });
                    }
                }
            }

            // The format required: {"role": "user", "content": [...]}
            messages.Add(new
            {
                role = "user",
                content = contentList
            });

            return messages;
        }
    }
}