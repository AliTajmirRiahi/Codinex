using Codify.Core.Conversation;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Core.Tools;
using Codify.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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

            await foreach (var item in StreamCompletionAsync(
                               provider,
                               model,
                               messages,
                               cancellationToken))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<ConversationEvent> ContinueAsync(
            IReadOnlyList<ChatMessage> messages,
            ChatMessage assistantMessage,
            IReadOnlyList<ToolResult> toolResults,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            var history = messages.ToList();

            history.Add(assistantMessage);

            history.AddRange(toolResults.Select(toolResult => new ChatMessage
            {
                Role = "tool",
                ToolCallId = toolResult.Id,
                Content = toolResult.Success
                    ? toolResult.Data.ToString()
                    : toolResult.Error
            }));

            await foreach (var item in StreamCompletionAsync(
                               provider,
                               model,
                               history,
                               cancellationToken))
            {
                yield return item;
            }
        }
        private async IAsyncEnumerable<ConversationEvent> StreamCompletionAsync(
                AiProvider provider,
                AiModel model,
                IReadOnlyList<ChatMessage> messages,
                [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var payload = BuildChatCompletionPayload(
                provider,
                model,
                messages,
                true);

            var tt = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

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
                        var arguments = ParseArguments(toolArguments.ToString());

                        var assistantMessage = new ChatMessage
                        {
                            Role = "assistant",
                            ToolCalls =
                            [
                                new ToolCall
                                {
                                    Id = toolCallId,
                                    Name = toolName,
                                    Arguments = arguments
                                }
                            ]
                        };

                        yield return ConversationEvent.ToolRequested(
                            new ToolRequest
                            {
                                Id = toolCallId,
                                Name = toolName,
                                Arguments = arguments
                            },
                            assistantMessage);
                    }
                    else
                    {
                        yield return ConversationEvent.Completed();
                    }

                    yield break;
                }

                var obj = jsonSerializer.Parse(json);

                var delta = obj["choices"]?[0]?["delta"];

                if (delta == null)
                    continue;

                var toolCall = delta["tool_calls"]?[0];

                if (toolCall != null)
                {
                    toolCallId ??= toolCall["id"]?.ToString();

                    var function = toolCall["function"];

                    if (function != null)
                    {
                        toolName ??= function["name"]?.ToString();

                        var arguments = function["arguments"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(arguments))
                        {
                            toolArguments.Append(arguments);
                        }
                    }

                    continue;
                }

                var content = delta["content"]?.ToString();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    yield return ConversationEvent.TextDelta(content);
                }
            }
        }
        private List<object> BuildMessages(IReadOnlyList<ChatMessage> prompts)
        {
            var messages = new List<object>();

            foreach (var prompt in prompts)
            {
                var role = NormalizeRole(prompt.Role);

                switch (role)
                {
                    case "tool":
                        messages.Add(new
                        {
                            role,
                            tool_call_id = prompt.ToolCallId,
                            content = prompt.Content
                        });

                        continue;
                    case "assistant" when
                        prompt.ToolCalls is { Count: > 0 }:
                        messages.Add(new
                        {
                            role,
                            tool_calls = prompt.ToolCalls.Select(x => new
                            {
                                id = x.Id,
                                type = "function",
                                function = new
                                {
                                    name = x.Name,
                                    arguments = x.Arguments.ToString()
                                }
                            })
                        });

                        continue;
                    default:
                        messages.Add(new
                        {
                            role,
                            content = prompt.Content
                        });
                        break;
                }
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
                                    type = ToJsonSchemaType(p.Value.Type),
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

        private static string ToJsonSchemaType(ToolPropertyType type)
        {
            return type switch
            {
                ToolPropertyType.String => "string",
                ToolPropertyType.Integer => "integer",
                ToolPropertyType.Number => "number",
                ToolPropertyType.Boolean => "boolean",
                ToolPropertyType.Object => "object",
                ToolPropertyType.Array => "array",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "user";

            return role.Trim().ToLowerInvariant() switch
            {
                "assistant" => "assistant",
                "system" => "system",
                "tool" => "tool",
                _ => "user"
            };
        }
    }
}