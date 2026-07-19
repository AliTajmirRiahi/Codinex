using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.Storage;
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

namespace Codify.Infrastructure.AI.Providers
{
    /// <summary>
    /// </summary>
    public class OpenAiCompatibleProvider(IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        IAiToolRegistry toolRegistry,
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

            var payload = BuildChatCompletionPayload(
                            provider,
                            model,
                            prompts,
                            true);

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

            var payload = BuildChatCompletionPayload(
                provider,
                model,
                messages,
                true);

            string toolCallId = null;
            string toolName = null;
            var toolArguments = new StringBuilder();

            await foreach (var json in client.StreamPostAsync(
                               provider,
                               "/chat/completions",
                               payload,
                               cancellationToken))
            {
                if (json == "[DONE]")
                {
                    if (!string.IsNullOrWhiteSpace(toolName))
                    {
                        yield return ConversationEvent.ToolRequested(
                            new ToolRequest
                            {
                                Id = toolCallId,
                                Name = toolName,
                                Arguments = ParseArguments(toolArguments.ToString())
                            });
                    }

                    yield break;
                }

                var obj = jsonSerializer.Parse(json);

                var delta = obj["choices"]?[0]?["delta"];

                if (delta == null)
                    continue;


                // Handle tool calls
                var toolCall = delta["tool_calls"]?[0];

                if (toolCall != null)
                {
                    toolCallId ??=
                        toolCall["id"]?.ToString();

                    var function = toolCall["function"];

                    if (function != null)
                    {
                        toolName ??=
                            function["name"]?.ToString();

                        var arguments =
                            function["arguments"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(arguments))
                        {
                            toolArguments.Append(arguments);
                        }
                    }

                    continue;
                }


                // Handle normal text streaming
                var content =
                    delta["content"]?.ToString();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    yield return ConversationEvent.TextDelta(content);
                }
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
        private object BuildChatCompletionPayload(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream)
        {
            return new
            {
                model = model.Id,
                messages = BuildMessages(messages),
                stream = model.SupportsStreaming == CapabilityProbeResult.Supported && stream,
                tools = BuildTools(),
                tool_choice = "auto"
            };
        }

        private object[] BuildTools()
        {
            return [.. toolRegistry
                .GetAll()
                .Select(tool => new
                {
                    type = "function",

                    function = new
                    {
                        name = tool.Name,

                        description = tool.Description,

                        parameters = new
                        {
                            type = "object",

                            properties = tool.Definition.Properties.ToDictionary(
                                p => p.Key,
                                p => new
                                {
                                    type = p.Value.Type,
                                    description = p.Value.Description
                                }),

                            required = tool.Definition.Required
                        }
                    }
                })];
        }
        private static JObject ParseArguments(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                return new JObject();

            try
            {
                return JObject.Parse(arguments);
            }
            catch
            {
                return new JObject
                {
                    ["raw"] = arguments
                };
            }
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