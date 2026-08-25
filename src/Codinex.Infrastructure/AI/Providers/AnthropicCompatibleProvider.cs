using Codinex.Core.Conversation;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Chat;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.Core.Workspace.Prompt;
using Codinex.Infrastructure.AI.Errors;
using Codinex.Storage.Managers;
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

namespace Codinex.Infrastructure.AI.Providers
{
    /// <summary>
    /// Implements Anthropic's Messages API against Codinex's provider abstractions.
    /// </summary>
    public class AnthropicCompatibleProvider(
        IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        IAiToolRegistry toolRegistry,
        IProviderClient client,
        IWorkspaceFileService workspaceFileService,
        IPromptProfiler promptProfiler,
        IPromptRecorder promptRecorder)
        : IAiProvider
    {
        private const string MessagesEndpoint = "/messages";

        private readonly ProviderManager _providerManager = providerManager;

        public async Task<string> SendAsync(
            IReadOnlyList<ChatMessage> prompts,
            string chatId = null,
            string chatMessageId = null,
            CancellationToken ct = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            var payload = BuildMessagesPayload(
                provider,
                model,
                prompts,
                false);

            try
            {
                var response = await client.PostAsync(
                    provider,
                    MessagesEndpoint,
                    payload,
                    ct);

                var payloadContent = Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented);

                await promptRecorder.RecordAsync(chatId, chatMessageId, payloadContent, ct);

                var json = jsonSerializer.Parse(response);

                if (TryCreateProviderErrorEvent(json, out _))
                {
                    throw new HttpRequestException("Provider returned an error response.");
                }

                var text = ReadTextContent(json);

                return !string.IsNullOrWhiteSpace(text)
                    ? text
                    : throw new HttpRequestException("No response content received.");
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
            string chatId = null,
            string chatMessageId = null,
            CancellationToken cancellationToken = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            return MapExpectedErrors(
                StreamMessagesAsync(
                    provider,
                    model,
                    messages,
                    chatId,
                    chatMessageId,
                    cancellationToken),
                cancellationToken);
        }

        public IAsyncEnumerable<ConversationEvent> ContinueAsync(
            IReadOnlyList<ChatMessage> history,
            string chatId = null,
            string chatMessageId = null,
            CancellationToken cancellationToken = default)
        {
            var model = _providerManager.ActiveModel;
            var provider = _providerManager.ActiveProvider;

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            return MapExpectedErrors(
                StreamMessagesAsync(
                    provider,
                    model,
                    history,
                    chatId,
                    chatMessageId,
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

        private async IAsyncEnumerable<ConversationEvent> StreamMessagesAsync(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            string chatId,
            string chatMessageId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var tools = BuildTools(messages);
            var payload = BuildMessagesPayload(
                provider,
                model,
                messages,
                true,
                tools);

            var promptProfile = BuildPromptProfile(messages, tools);
            var payloadContent = Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented) +
                                 Environment.NewLine +
                                 Environment.NewLine +
                                 FormatPromptProfile(promptProfile);

            await promptRecorder.RecordAsync(chatId, chatMessageId, payloadContent, cancellationToken);

            Dictionary<int, ToolCallBuilder> toolCalls = [];

            await foreach (var json in client.StreamPostAsync(
                               provider,
                               MessagesEndpoint,
                               payload,
                               cancellationToken))
            {
                if (json == "[DONE]")
                {
                    yield return CreateTerminalEvent(toolCalls);
                    yield break;
                }

                var obj = jsonSerializer.Parse(json);

                if (TryCreateProviderErrorEvent(obj, out var providerErrorEvent))
                {
                    yield return providerErrorEvent;
                    yield break;
                }

                switch (obj["type"]?.ToString())
                {
                    case "message_start":
                    case "ping":
                        // Anthropic usage is present on message_start/message_delta, but Codinex has no usage event yet.
                        continue;

                    case "content_block_start":
                        ReadContentBlockStart(obj, toolCalls);
                        continue;

                    case "content_block_delta":
                        var contentEvent = ReadContentBlockDelta(obj, toolCalls);

                        if (contentEvent != null)
                        {
                            yield return contentEvent;
                        }

                        continue;

                    case "message_delta":
                        // Anthropic usage deltas are intentionally not emitted until Codinex exposes a usage event type.
                        continue;

                    case "message_stop":
                        yield return CreateTerminalEvent(toolCalls);
                        yield break;
                }
            }

            yield return CreateTerminalEvent(toolCalls);
        }

        private JObject BuildMessagesPayload(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream)
        {
            return BuildMessagesPayload(
                provider,
                model,
                messages,
                stream,
                BuildTools(messages));
        }

        private JObject BuildMessagesPayload(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream,
            object[] tools)
        {
            var payload = new JObject
            {
                ["model"] = model.Id,
                ["max_tokens"] = GetMaxOutputTokens(model),
                ["stream"] = model.SupportsStreaming == CapabilityProbeResult.Supported && stream,
                ["messages"] = JArray.FromObject(BuildMessages(messages))
            };

            var system = BuildSystem(messages);

            if (!string.IsNullOrWhiteSpace(system))
            {
                payload["system"] = system;
            }

            if (tools is { Length: > 0 })
            {
                payload["tools"] = JArray.FromObject(tools);
                payload["tool_choice"] = JObject.FromObject(new { type = "auto" });
            }

            return payload;
        }

        private static int GetMaxOutputTokens(AiModel model)
        {
            return model.TokenLimit <= 1
                ? 4096
                : Math.Min(model.TokenLimit, 4096);
        }

        private List<object> BuildMessages(IReadOnlyList<ChatMessage> prompts)
        {
            var messages = new List<object>();

            foreach (var prompt in prompts)
            {
                var role = NormalizeRole(prompt.Role);

                switch (role)
                {
                    case "system":
                        continue;

                    case "tool":
                        messages.Add(new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "tool_result",
                                    tool_use_id = prompt.ToolCallId,
                                    content = prompt.Content ?? string.Empty
                                }
                            }
                        });

                        continue;

                    case "assistant" when prompt.ToolCalls is { Count: > 0 }:
                        messages.Add(new
                        {
                            role,
                            content = BuildAssistantToolUseContent(prompt)
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

        private static object[] BuildAssistantToolUseContent(ChatMessage prompt)
        {
            var content = new List<object>();

            if (!string.IsNullOrWhiteSpace(prompt.Content))
            {
                content.Add(new
                {
                    type = "text",
                    text = prompt.Content
                });
            }

            content.AddRange(prompt.ToolCalls.Select(x => new
            {
                type = "tool_use",
                id = x.Id,
                name = x.Name,
                input = x.Arguments ?? new JObject()
            }));

            return content.ToArray();
        }

        private static object BuildMessageContent(ChatMessage prompt)
        {
            var content = new List<object>();

            if (!string.IsNullOrWhiteSpace(prompt.Content))
            {
                content.Add(new
                {
                    type = "text",
                    text = prompt.Content
                });
            }

            if (prompt.Data?["images"] is JArray images)
            {
                foreach (var image in images)
                {
                    var base64 = image["base64"]?.ToString();

                    if (string.IsNullOrWhiteSpace(base64)) continue;

                    content.Add(BuildImageContentBlock(
                        base64,
                        image["mimeType"]?.ToString()));
                }
            }

            return content.Count == 0
                ? string.Empty
                : content.ToArray();
        }

        private static object BuildImageContentBlock(string base64, string mimeType)
        {
            var mediaType = string.IsNullOrWhiteSpace(mimeType)
                ? "image/png"
                : mimeType;
            var data = base64;

            if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = base64.IndexOf(',');

                if (commaIndex >= 0)
                {
                    data = base64.Substring(commaIndex + 1);
                    var metadata = base64.Substring(5, commaIndex - 5);
                    var semicolonIndex = metadata.IndexOf(';');

                    mediaType = semicolonIndex >= 0
                        ? metadata.Substring(0, semicolonIndex)
                        : metadata;
                }
            }

            return new
            {
                type = "image",
                source = new
                {
                    type = "base64",
                    media_type = mediaType,
                    data
                }
            };
        }

        private static string BuildSystem(IReadOnlyList<ChatMessage> messages)
        {
            return string.Join(
                Environment.NewLine + Environment.NewLine,
                messages
                    .Where(x => NormalizeRole(x.Role) == "system")
                    .Select(x => x.Content)
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private object[] BuildTools(IReadOnlyList<ChatMessage> messages)
        {
            var tools = toolRegistry
                .GetAll()
                .Where(x => x.Visibility == ToolVisibility.Model);

            var plannedTools = GetPlannedTools(messages);

            if (plannedTools is { Count: > 0 })
            {
                var plannedToolNames = new HashSet<string>(
                    plannedTools,
                    StringComparer.OrdinalIgnoreCase);

                tools = tools.Where(x => plannedToolNames.Contains(x.Name));
            }

            return
            [
                .. tools.Select(tool => new
                {
                    name = tool.Name,

                    description = tool.Description,

                    input_schema = new
                    {
                        type = "object",

                        properties = tool.Definition.Properties.ToDictionary(
                            p => p.Key,
                            p => p.Value.ToJsonSchema()),

                        required = tool.Definition.Required
                    }
                })
            ];
        }

        private static IReadOnlyList<string> GetPlannedTools(IReadOnlyList<ChatMessage> messages)
        {
            return messages?
                .Select(x => x.Context?.PlannedTools)
                .FirstOrDefault(x => x != null);
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
                return JObject.FromObject(tool)["name"]?.ToString() ?? "Tool";
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

        private static void ReadContentBlockStart(
            JToken obj,
            IDictionary<int, ToolCallBuilder> toolCalls)
        {
            var contentBlock = obj["content_block"];

            if (contentBlock?["type"]?.ToString() != "tool_use")
            {
                return;
            }

            var index = obj.Value<int?>("index") ?? toolCalls.Count;

            if (!toolCalls.TryGetValue(index, out var builder))
            {
                builder = new ToolCallBuilder { Index = index };

                toolCalls[index] = builder;
            }

            builder.Id ??= contentBlock["id"]?.ToString();
            builder.Name ??= contentBlock["name"]?.ToString();

            if (contentBlock["input"] is JObject input && input.HasValues)
            {
                builder.Arguments.Append(input.ToString(Newtonsoft.Json.Formatting.None));
            }
        }

        private static ConversationEvent ReadContentBlockDelta(
            JToken obj,
            IDictionary<int, ToolCallBuilder> toolCalls)
        {
            var delta = obj["delta"];
            var deltaType = delta?["type"]?.ToString();

            switch (deltaType)
            {
                case "text_delta":
                    var text = delta["text"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (TryCreateProviderErrorEvent(text, out var providerErrorEvent))
                        {
                            return providerErrorEvent;
                        }

                        return ConversationEvent.TextDelta(text);
                    }

                    break;

                case "input_json_delta":
                    var partialJson = delta["partial_json"]?.ToString();

                    if (!string.IsNullOrEmpty(partialJson))
                    {
                        var index = obj.Value<int?>("index") ?? toolCalls.Count;

                        if (!toolCalls.TryGetValue(index, out var builder))
                        {
                            builder = new ToolCallBuilder { Index = index };

                            toolCalls[index] = builder;
                        }

                        builder.Arguments.Append(partialJson);
                    }

                    break;
            }

            return null;
        }

        private static ConversationEvent CreateTerminalEvent(Dictionary<int, ToolCallBuilder> toolCalls)
        {
            if (toolCalls.Count == 0)
            {
                return ConversationEvent.Completed();
            }

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

            return ConversationEvent.ToolRequested(
                requests,
                assistantMessage);
        }

        private static string ReadTextContent(JToken root)
        {
            if (root?["content"] is not JArray content)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            foreach (var block in content)
            {
                if (block["type"]?.ToString() == "text")
                {
                    sb.Append(block["text"]?.ToString());
                }
            }

            return sb.ToString();
        }

        private static bool TryCreateProviderErrorEvent(
            JToken token,
            out ConversationEvent providerErrorEvent)
        {
            providerErrorEvent = null;

            if (token == null)
            {
                return false;
            }

            var error = token["error"] ?? token["Error"];

            if (error == null)
            {
                return false;
            }

            providerErrorEvent = AiErrorFactory.ToConversationEvent(
                AiErrorFactory.FromProviderErrorBody(
                    token.ToString(Newtonsoft.Json.Formatting.None)));

            return true;
        }

        private static bool TryCreateProviderErrorEvent(
            string content,
            out ConversationEvent providerErrorEvent)
        {
            providerErrorEvent = null;

            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var trimmedContent = content.Trim();

            if (!trimmedContent.StartsWith("{") && !trimmedContent.StartsWith("["))
            {
                return false;
            }

            try
            {
                return TryCreateProviderErrorEvent(
                    JToken.Parse(trimmedContent),
                    out providerErrorEvent);
            }
            catch
            {
                return false;
            }
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
                "tool" => "tool",
                _ => "user"
            };
        }
    }
}
