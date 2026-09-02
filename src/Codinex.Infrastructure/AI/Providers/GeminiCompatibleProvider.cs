using Codinex.Core.Conversation;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Chat;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models.AI;
using Codinex.Core.Models.Chat;
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
    /// Implements Google's Gemini "generateContent" REST API against Codinex's provider abstractions.
    /// Mirrors <see cref="AnthropicCompatibleProvider"/>: a separate system instruction, content
    /// broken into typed parts, and streamed tool calls aggregated into a single terminal event.
    /// </summary>
    public class GeminiCompatibleProvider(
        IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        IAiToolRegistry toolRegistry,
        IProviderClient client,
        IPromptProfiler promptProfiler,
        IPromptRecorder promptRecorder)
        : IAiProvider
    {
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

            var payload = BuildGenerateContentPayload(
                model,
                prompts,
                false);

            try
            {
                var response = await client.PostAsync(
                    provider,
                    GetGenerateEndpoint(model, false),
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
                StreamGenerateContentAsync(
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
                StreamGenerateContentAsync(
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

        private async IAsyncEnumerable<ConversationEvent> StreamGenerateContentAsync(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            string chatId,
            string chatMessageId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var tools = BuildTools(messages);
            var payload = BuildGenerateContentPayload(
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
            var isThinking = false;

            await foreach (var json in client.StreamPostAsync(
                               provider,
                               GetGenerateEndpoint(model, true),
                               payload,
                               cancellationToken))
            {
                if (json == "[DONE]")
                {
                    break;
                }

                var obj = jsonSerializer.Parse(json);

                if (TryCreateProviderErrorEvent(obj, out var providerErrorEvent))
                {
                    yield return providerErrorEvent;
                    yield break;
                }

                var parts = obj["candidates"]?[0]?["content"]?["parts"] as JArray;

                if (parts == null)
                {
                    continue;
                }

                foreach (var part in parts)
                {
                    if (part["functionCall"] is JObject functionCall)
                    {
                        if (isThinking)
                        {
                            isThinking = false;

                            yield return ConversationEvent.ThinkingCompleted();
                        }

                        AddFunctionCall(functionCall, toolCalls);

                        continue;
                    }

                    var text = part["text"]?.ToString();

                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    var isThought = part["thought"]?.Value<bool>() == true;

                    if (isThought)
                    {
                        if (!isThinking)
                        {
                            isThinking = true;

                            yield return ConversationEvent.ThinkingStarted();
                        }

                        yield return ConversationEvent.ThinkingUpdated(text);

                        continue;
                    }

                    if (isThinking)
                    {
                        isThinking = false;

                        yield return ConversationEvent.ThinkingCompleted();
                    }

                    if (TryCreateProviderErrorEvent(text, out var textErrorEvent))
                    {
                        yield return textErrorEvent;
                        yield break;
                    }

                    yield return ConversationEvent.TextDelta(text);
                }
            }

            if (isThinking)
            {
                yield return ConversationEvent.ThinkingCompleted();
            }

            yield return CreateTerminalEvent(toolCalls);
        }

        private JObject BuildGenerateContentPayload(
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream)
        {
            return BuildGenerateContentPayload(
                model,
                messages,
                stream,
                BuildTools(messages));
        }

        private JObject BuildGenerateContentPayload(
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream,
            object[] tools)
        {
            // "stream" only selects the endpoint (":streamGenerateContent"); Gemini has no body flag.
            _ = stream;

            var payload = new JObject
            {
                ["contents"] = JArray.FromObject(BuildContents(messages)),
                ["generationConfig"] = new JObject
                {
                    ["maxOutputTokens"] = GetMaxOutputTokens(model)
                }
            };

            var systemInstruction = BuildSystemInstruction(messages);

            if (systemInstruction != null)
            {
                payload["systemInstruction"] = systemInstruction;
            }

            if (tools is { Length: > 0 })
            {
                payload["tools"] = JArray.FromObject(tools);
                payload["toolConfig"] = JObject.FromObject(new
                {
                    functionCallingConfig = new { mode = "AUTO" }
                });
            }

            return payload;
        }

        private static int GetMaxOutputTokens(AiModel model)
        {
            return model.TokenLimit <= 1
                ? 4096
                : Math.Min(model.TokenLimit, 4096);
        }

        private static string GetGenerateEndpoint(AiModel model, bool stream)
        {
            return stream
                ? $"/models/{model.Id}:streamGenerateContent?alt=sse"
                : $"/models/{model.Id}:generateContent";
        }

        private List<object> BuildContents(IReadOnlyList<ChatMessage> prompts)
        {
            var contents = new List<object>();
            var toolNamesById = BuildToolNameLookup(prompts);

            foreach (var prompt in prompts)
            {
                var role = NormalizeRole(prompt.Role);

                switch (role)
                {
                    case "system":
                        continue;

                    case "tool":
                        var functionName = !string.IsNullOrWhiteSpace(prompt.ToolCallId)
                                           && toolNamesById.TryGetValue(prompt.ToolCallId, out var mappedName)
                            ? mappedName
                            : prompt.ToolCallId ?? string.Empty;

                        contents.Add(new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new
                                {
                                    functionResponse = new
                                    {
                                        name = functionName,
                                        response = new { result = prompt.Content ?? string.Empty }
                                    }
                                }
                            }
                        });

                        continue;

                    case "assistant" when prompt.ToolCalls is { Count: > 0 }:
                        contents.Add(new
                        {
                            role = "model",
                            parts = BuildAssistantFunctionCallParts(prompt)
                        });

                        continue;

                    case "assistant":
                        contents.Add(new
                        {
                            role = "model",
                            parts = BuildMessageParts(prompt)
                        });

                        break;

                    default:
                        contents.Add(new
                        {
                            role = "user",
                            parts = BuildMessageParts(prompt)
                        });

                        break;
                }
            }

            return contents;
        }

        private static Dictionary<string, string> BuildToolNameLookup(IReadOnlyList<ChatMessage> prompts)
        {
            var lookup = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var prompt in prompts)
            {
                if (prompt.ToolCalls == null) continue;

                foreach (var call in prompt.ToolCalls)
                {
                    if (!string.IsNullOrWhiteSpace(call.Id) && !string.IsNullOrWhiteSpace(call.Name))
                    {
                        lookup[call.Id] = call.Name;
                    }
                }
            }

            return lookup;
        }

        private static object[] BuildAssistantFunctionCallParts(ChatMessage prompt)
        {
            var parts = new List<object>();

            if (!string.IsNullOrWhiteSpace(prompt.Content))
            {
                parts.Add(new { text = prompt.Content });
            }

            parts.AddRange(prompt.ToolCalls.Select(x => new
            {
                functionCall = new
                {
                    name = x.Name,
                    args = (object)(x.Arguments ?? new JObject())
                }
            }));

            return parts.ToArray();
        }

        private static object[] BuildMessageParts(ChatMessage prompt)
        {
            var parts = new List<object>();

            if (!string.IsNullOrWhiteSpace(prompt.Content))
            {
                parts.Add(new { text = prompt.Content });
            }

            if (prompt.Data?["images"] is JArray images)
            {
                foreach (var image in images)
                {
                    var base64 = image["base64"]?.ToString();

                    if (string.IsNullOrWhiteSpace(base64)) continue;

                    parts.Add(BuildInlineDataPart(
                        base64,
                        image["mimeType"]?.ToString()));
                }
            }

            return parts.Count == 0
                ? new object[] { new { text = string.Empty } }
                : parts.ToArray();
        }

        private static object BuildInlineDataPart(string base64, string mimeType)
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
                inlineData = new
                {
                    mimeType = mediaType,
                    data
                }
            };
        }

        private static JObject BuildSystemInstruction(IReadOnlyList<ChatMessage> messages)
        {
            var system = string.Join(
                Environment.NewLine + Environment.NewLine,
                messages
                    .Where(x => NormalizeRole(x.Role) == "system")
                    .Select(x => x.Content)
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(system))
            {
                return null;
            }

            return new JObject
            {
                ["parts"] = new JArray { new JObject { ["text"] = system } }
            };
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

            var declarations = tools.Select(tool =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = tool.Definition.Properties.ToDictionary(
                        p => p.Key,
                        p => p.Value.ToJsonSchema())
                };

                if (tool.Definition.Required is { Count: > 0 })
                {
                    parameters["required"] = tool.Definition.Required;
                }

                return new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters
                };
            }).ToArray();

            return declarations.Length == 0
                ? []
                : [new { functionDeclarations = declarations }];
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
                var declarations = JObject.FromObject(tool)["functionDeclarations"] as JArray;

                return declarations?.FirstOrDefault()?["name"]?.ToString() ?? "Tool";
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

        private static void AddFunctionCall(
            JObject functionCall,
            IDictionary<int, ToolCallBuilder> toolCalls)
        {
            var index = toolCalls.Count;

            var builder = new ToolCallBuilder
            {
                Index = index,
                Id = $"call_{index}",
                Name = functionCall["name"]?.ToString()
            };

            if (functionCall["args"] is JToken args && args.Type != JTokenType.Null)
            {
                builder.Arguments.Append(args.ToString(Newtonsoft.Json.Formatting.None));
            }

            toolCalls[index] = builder;
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
            if (root?["candidates"]?[0]?["content"]?["parts"] is not JArray parts)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            foreach (var part in parts)
            {
                if (part["thought"]?.Value<bool>() == true)
                {
                    continue;
                }

                sb.Append(part["text"]?.ToString());
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
                "model" => "assistant",
                "system" => "system",
                "tool" => "tool",
                _ => "user"
            };
        }
    }
}
