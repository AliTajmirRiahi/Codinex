using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.Core.Workspace.Prompt;
using Codinex.Storage.Managers;

namespace Codinex.Infrastructure.AI.Providers
{
    public sealed class ToolCallBuilder
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int Index { get; set; }

        public StringBuilder Arguments { get; } = new();

    }
    /// <summary>
    /// </summary>
    public class OpenAiCompatibleProvider(IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        IAiToolRegistry toolRegistry,
        IOpenAiCompatibleClient client,
        IWorkspaceFileService workspaceFileService,
        IPromptProfiler promptProfiler)
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
            IReadOnlyList<ChatMessage> history,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

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
            var tools = BuildTools();
            var payload = BuildChatCompletionPayload(
                provider,
                model,
                messages,
                true,
                tools);
#if DEBUG
            var promptProfile = BuildPromptProfile(messages, tools);
            var payloadContent = Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented) +
                                 Environment.NewLine +
                                 Environment.NewLine +
                                 FormatPromptProfile(promptProfile);

            var path = @$"C:\Users\Programmer\AppData\Local\Codinex\prompts\prompt_{Guid.NewGuid()}.json";

            await workspaceFileService.CreateFileAsync(path, cancellationToken);

            await workspaceFileService.WriteAsync(path, payloadContent, cancellationToken: cancellationToken);
#endif
            Dictionary<int, ToolCallBuilder> toolCalls = [];
            var toolArguments = new StringBuilder();

            await foreach (var json in client.StreamPostAsync(
                               provider,
                               "/chat/completions",
                               payload,
                               cancellationToken))
            {
                if (json == "[DONE]")
                {
                    if (toolCalls.Count > 0)
                    {
                        var requests = new List<ToolRequest>();
                        var assistantToolCalls = new List<ToolCall>();

                        foreach (var builder in toolCalls.Values.OrderBy(x => x.Index))
                        {
                            var arguments = ParseArguments(builder.Arguments.ToString());

                            assistantToolCalls.Add(new ToolCall
                            {
                                Id = builder.Id,
                                Name = builder.Name,
                                Arguments = arguments
                            });

                            requests.Add(new ToolRequest
                            {
                                Id = builder.Id,
                                Name = builder.Name,
                                Arguments = arguments
                            });
                        }

                        var assistantMessage = new ChatMessage
                        {
                            Role = "assistant",
                            ToolCalls = assistantToolCalls
                        };

                        yield return ConversationEvent.ToolRequested(
                            requests,
                            assistantMessage);
                    }
                    else
                    {
                        yield return ConversationEvent.Completed();
                    }

                    yield break;
                }

                var obj = jsonSerializer.Parse(json);

                var choices = obj["choices"];

                if (choices is not JArray array || array.Count == 0)
                {
                    continue;
                }

                var delta = array[0]?["delta"];

                if (delta == null)
                {
                    continue;
                }

                if (delta["tool_calls"] is JArray toolCallsArray)
                {
                    foreach (var toolCall in toolCallsArray)
                    {
                        var index = toolCall.Value<int>("index");

                        if (!toolCalls.TryGetValue(index, out var builder))
                        {
                            builder = new ToolCallBuilder() { Index = index };

                            toolCalls[index] = builder;
                        }

                        builder.Id ??= toolCall["id"]?.ToString();

                        var function = toolCall["function"];

                        if (function == null) continue;

                        builder.Name ??= function["name"]?.ToString();

                        var arguments = function["arguments"]?.ToString();

                        if (!string.IsNullOrEmpty(arguments))
                        {
                            builder.Arguments.Append(arguments);
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
                            content = BuildMessageContent(prompt)
                        });
                        break;
                }
            }

            return messages;
        }

        private static object BuildMessageContent(ChatMessage prompt)
        {
            if (prompt.Data?["images"] is not JArray images || images.Count == 0)
            {
                return prompt.Content;
            }

            var content = new List<object>
            {
                new
                {
                    type = "text",
                    text = prompt.Content ?? string.Empty
                }
            };

            foreach (var image in images)
            {
                var base64 = image["base64"]?.ToString();

                if (string.IsNullOrWhiteSpace(base64)) continue;

                var mimeType = image["mimeType"]?.ToString();
                var imageUrl = base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                    ? base64
                    : $"data:{(string.IsNullOrWhiteSpace(mimeType) ? "image/png" : mimeType)};base64,{base64}";

                content.Add(new
                {
                    type = "image_url",
                    image_url = new
                    {
                        url = imageUrl
                    }
                });
            }

            return content;
        }

        private object BuildChatCompletionPayload(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream)
        {
            return BuildChatCompletionPayload(
                provider,
                model,
                messages,
                stream,
                BuildTools());
        }

        private object BuildChatCompletionPayload(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream,
            object[] tools)
        {
            return new
            {
                model = model.Id,
                messages = BuildMessages(messages),
                stream = model.SupportsStreaming == CapabilityProbeResult.Supported && stream,
                tools,
                tool_choice = "auto"
            };
        }

        private object[] BuildTools()
        {
            return
            [
                .. toolRegistry
                    .GetAll()
                    .Where(x => x.Visibility == ToolVisibility.Model)
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
                                    p => p.Value.ToJsonSchema()),

                                required = tool.Definition.Required
                            }
                        }
                    })
            ];
        }
        private PromptProfileResult BuildPromptProfile(IReadOnlyList<ChatMessage> messages, object[] tools)
        {
            var sections = new List<PromptSectionProfile>();
            var existingProfile = messages
                .Select(x => x.Context?.PromptProfile)
                .FirstOrDefault(x => x != null);

            if (existingProfile?.Sections != null)
            {
                sections.AddRange(existingProfile.Sections);
            }

            var toolsContext = new PromptContext();
            var toolsSection = new PromptContextSection
            {
                Name = "Tools"
            };

            foreach (var tool in tools ?? Array.Empty<object>())
            {
                toolsSection.Items.Add(new PromptContextItem
                {
                    Title = GetToolName(tool),
                    Content = Newtonsoft.Json.JsonConvert.SerializeObject(tool),
                    Reason = "Registered model tool definition"
                });
            }

            if (toolsSection.Items.Count > 0)
            {
                toolsContext.Sections.Add(toolsSection);
            }

            var toolsProfile = promptProfiler.Profile(toolsContext);

            if (toolsProfile.Sections != null)
            {
                sections.AddRange(toolsProfile.Sections);
            }

            var totalCharacters = sections.Sum(x => x.Characters);

            ApplySectionPercentages(sections, totalCharacters);

            return new PromptProfileResult
            {
                Sections = sections,
                TotalCharacters = totalCharacters,
                EstimatedTokens = totalCharacters / 4
            };
        }

        private static string FormatPromptProfile(PromptProfileResult profile)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=========================");
            sb.AppendLine("Request Statistics");
            sb.AppendLine("=========================");
            sb.AppendLine();

            foreach (var section in profile.Sections)
            {
                AppendPromptSection(sb, section, 0);
                sb.AppendLine();
            }

            sb.AppendLine("-------------------------");
            sb.AppendLine($"Total Characters : {FormatNumber(profile.TotalCharacters)}");
            sb.AppendLine($"Total Tokens : {FormatNumber(profile.EstimatedTokens)}");

            return sb.ToString().TrimEnd();
        }

        private static void AppendPromptSection(StringBuilder sb, PromptSectionProfile section, int depth)
        {
            var indent = new string(' ', depth * 2);

            sb.AppendLine($"{indent}{section.Name}");
            sb.AppendLine($"{indent}Characters : {FormatNumber(section.Characters)}");
            sb.AppendLine($"{indent}Tokens : {FormatNumber(section.EstimatedTokens)}");
            sb.AppendLine($"{indent}Percentage : {FormatPercentage(section.SectionPercentage)}");

            if (!string.IsNullOrWhiteSpace(section.Reason))
            {
                sb.AppendLine($"{indent}Reason : {section.Reason}");
            }

            if (section.Children == null || section.Children.Count == 0)
            {
                return;
            }

            foreach (var child in section.Children)
            {
                sb.AppendLine();
                AppendPromptSection(sb, child, depth + 1);
            }
        }

        private static void ApplySectionPercentages(IEnumerable<PromptSectionProfile> sections, int totalCharacters)
        {
            foreach (var section in sections ?? Enumerable.Empty<PromptSectionProfile>())
            {
                section.SectionPercentage = totalCharacters == 0
                    ? 0
                    : (double)section.Characters / totalCharacters * 100;

                ApplySectionPercentages(section.Children, totalCharacters);
            }
        }

        private static string GetToolName(object tool)
        {
            if (tool == null)
            {
                return "Tool";
            }

            try
            {
                return JObject.FromObject(tool)["function"]?["name"]?.ToString() ?? "Tool";
            }
            catch
            {
                return "Tool";
            }
        }

        private static string FormatNumber(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string FormatPercentage(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
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