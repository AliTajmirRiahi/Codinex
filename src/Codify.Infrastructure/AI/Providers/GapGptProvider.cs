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
    public class OpenAiCompatibleProvider(IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        IOpenAiCompatibleClient client)
        : IAiProvider
    {
        private readonly ProviderManager _providerManager = providerManager;

        public async Task<string> SendAsync(
            IReadOnlyList<ChatMessage> prompts,
            CancellationToken ct = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            var payload = new
            {
                model = model.Id,
                messages = BuildMessages(prompts),
                stream = false
            };

            var response = await client.PostAsync(
                provider,
                "/chat/completions",
                payload,
                ct);

            var json = jsonSerializer.Parse(response);

            return json["choices"]?[0]?["message"]?["content"]?.ToString()
                   ?? throw new HttpRequestException("No response content received.");
        }

        public async IAsyncEnumerable<ConversationEvent> SendStreamAsync(
            IReadOnlyList<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            var payload = new
            {
                model = model.Id,
                messages = BuildMessages(messages),
                stream = true
            };

            await foreach (var json in client.StreamPostAsync(
                               provider,
                               "/chat/completions",
                               payload,
                               cancellationToken))
            {
                if (json == "[DONE]")
                    yield break;

                var obj = jsonSerializer.Parse(json);

                var chunk = obj["choices"]?[0]?["delta"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(chunk))
                    continue;

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