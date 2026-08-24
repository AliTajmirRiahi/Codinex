using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;
using Codinex.Infrastructure.AI.Errors;
using Codinex.Storage.Managers;

namespace Codinex.Infrastructure.AI.Providers
{
    /// <summary>
    /// Native Ollama provider using the local /api/chat endpoint.
    /// </summary>
    public class OllamaProvider(
        IJsonSerializer jsonSerializer,
        ProviderManager providerManager,
        SettingsManager settingsManager,
        IAiToolRegistry toolRegistry,
        IProviderClient client,
        IWorkspaceFileService workspaceFileService,
        IPromptRecorder promptRecorder)
        : IAiPreprocessorProvider
    {
        private readonly ProviderManager _providerManager = providerManager;
        private readonly SettingsManager _settingsManager = settingsManager;

        public async Task<string> SendAsync(
            IReadOnlyList<ChatMessage> prompt,
            string chatId = null,
            string chatMessageId = null,
            CancellationToken ct = default)
        {
            var provider = GetProvider();
            var model = GetModel(provider);

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            try
            {
                var response = await client.PostAsync(
                    provider,
                    "/api/chat",
                    BuildChatPayload(
                        provider,
                        model,
                        prompt,
                        false),
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
            string chatId = null,
            string chatMessageId = null,
            CancellationToken cancellationToken = default)
        {
            var provider = GetProvider();
            var model = GetModel(provider);

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            return MapExpectedErrors(
                StreamChatAsync(
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
            var provider = GetProvider();
            var model = GetModel(provider);

            if (provider == null || model == null)
                throw new ArgumentException("Provider or Model is not configured correctly.");

            return MapExpectedErrors(
                StreamChatAsync(
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

        private async IAsyncEnumerable<ConversationEvent> StreamChatAsync(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            string chatId,
            string chatMessageId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var toolCalls = new Dictionary<string, ToolCall>();
            var isThinking = false;

            var payload = BuildChatPayload(
                provider,
                model,
                messages,
                true);

            var payloadContent = Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented);

            await promptRecorder.RecordAsync(chatId, chatMessageId, payloadContent, cancellationToken);

            await foreach (var line in client.StreamPostAsync(
                               provider,
                               "/api/chat",
                               payload,
                               cancellationToken))
            {
                var json = jsonSerializer.Parse(line);

                foreach (var toolCall in ReadToolCalls(json))
                {
                    if (string.IsNullOrWhiteSpace(toolCall.Id))
                    {
                        toolCall.Id = $"call_{toolCalls.Count}";
                    }

                    toolCalls[toolCall.Id] = toolCall;
                }

                var thinking = json["message"]?["thinking"]?.ToString()
                    ?? json["message"]?["reasoning_content"]?.ToString()
                    ?? json["message"]?["reasoning"]?.ToString();

                if (!string.IsNullOrEmpty(thinking))
                {
                    if (!isThinking)
                    {
                        isThinking = true;

                        yield return ConversationEvent.ThinkingStarted();
                    }

                    yield return ConversationEvent.ThinkingUpdated(thinking);
                }

                var content = json["message"]?["content"]?.ToString();

                if (!string.IsNullOrEmpty(content))
                {
                    if (isThinking)
                    {
                        isThinking = false;

                        yield return ConversationEvent.ThinkingCompleted();
                    }

                    yield return ConversationEvent.TextDelta(content);
                }

                if (json.Value<bool?>("done") == true)
                {
                    if (isThinking)
                    {
                        isThinking = false;

                        yield return ConversationEvent.ThinkingCompleted();
                    }

                    if (toolCalls.Count > 0)
                    {
                        var assistantToolCalls = toolCalls.Values.ToList();
                        var requests = assistantToolCalls
                            .Select(x => new ToolRequest
                            {
                                Id = x.Id,
                                Name = x.Name,
                                Arguments = x.Arguments ?? new JObject()
                            })
                            .ToList();

                        yield return ConversationEvent.ToolRequested(
                            requests,
                            new ChatMessage
                            {
                                Role = "assistant",
                                ToolCalls = assistantToolCalls
                            });
                    }
                    else
                    {
                        yield return ConversationEvent.Completed();
                    }

                    yield break;
                }
            }

            yield return ConversationEvent.Completed();
        }

        private object BuildChatPayload(
            AiProvider provider,
            AiModel model,
            IReadOnlyList<ChatMessage> messages,
            bool stream)
        {
            var tools = BuildTools(provider, messages);

            return tools.Length == 0
                ? new
                {
                    model = model.Id,
                    messages = BuildMessages(messages),
                    stream
                }
                : new
                {
                    model = model.Id,
                    messages = BuildMessages(messages),
                    stream,
                    tools
                };
        }

        private static List<object> BuildMessages(IReadOnlyList<ChatMessage> prompts)
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
                            content = prompt.Content ?? string.Empty
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
                                    arguments = x.Arguments ?? new JObject()
                                }
                            }),
                            content = prompt.Content ?? string.Empty
                        });

                        continue;
                    default:
                        break;
                }

                var content = prompt.Content ?? string.Empty;
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

        private object[] BuildTools(
            AiProvider provider,
            IReadOnlyList<ChatMessage> messages)
        {
            if (!IsPrimaryProvider(provider))
            {
                return [];
            }

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

        private bool IsPrimaryProvider(AiProvider provider)
        {
            var activeProvider = _providerManager.ActiveProvider;

            return activeProvider?.IsLocal == true &&
                   string.Equals(activeProvider.Id, provider?.Id, StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> GetPlannedTools(IReadOnlyList<ChatMessage> messages)
        {
            return messages?
                .Select(x => x.Context?.PlannedTools)
                .FirstOrDefault(x => x != null);
        }

        private static IEnumerable<ToolCall> ReadToolCalls(JObject json)
        {
            if (json?["message"]?["tool_calls"] is not JArray toolCalls)
            {
                yield break;
            }

            var index = 0;

            foreach (var toolCall in toolCalls)
            {
                var function = toolCall["function"];
                var name = function?["name"]?.ToString()
                           ?? toolCall["name"]?.ToString();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                yield return new ToolCall
                {
                    Id = toolCall["id"]?.ToString() ?? $"call_{index++}",
                    Name = name,
                    Arguments = ParseArguments(
                        function?["arguments"] ??
                        toolCall["arguments"])
                };
            }
        }

        private static JObject ParseArguments(JToken arguments)
        {
            if (arguments == null)
                return new JObject();

            if (arguments is JObject obj)
                return obj;

            if (arguments.Type == JTokenType.String)
            {
                var value = arguments.ToString();

                if (string.IsNullOrWhiteSpace(value))
                    return new JObject();

                try
                {
                    return JObject.Parse(value);
                }
                catch
                {
                    return new JObject
                    {
                        ["raw"] = value
                    };
                }
            }

            return JObject.FromObject(arguments);
        }

        private AiProvider GetProvider()
        {
            var activeProvider = _providerManager.ActiveProvider;

            if (activeProvider?.IsLocal == true)
            {
                return activeProvider;
            }

            var settings = _settingsManager.Settings;

            if (settings?.EnablePreprocessorAi != true ||
                string.IsNullOrWhiteSpace(settings.PreprocessorAiProviderId))
            {
                return null;
            }

            return _providerManager.Providers
                .FirstOrDefault(x =>
                    x?.IsLocal == true &&
                    string.Equals(x.Id, settings.PreprocessorAiProviderId, StringComparison.OrdinalIgnoreCase));
        }

        private AiModel GetModel(AiProvider provider)
        {
            if (provider == null)
            {
                return null;
            }

            var activeProvider = _providerManager.ActiveProvider;

            if (activeProvider?.IsLocal == true &&
                string.Equals(activeProvider.Id, provider.Id, StringComparison.OrdinalIgnoreCase))
            {
                return provider.Models?.FirstOrDefault(x => x.IsCurrent)
                       ?? provider.Models?.FirstOrDefault(x => x.IsSelected);
            }

            var settings = _settingsManager.Settings;

            if (settings?.EnablePreprocessorAi == true &&
                !string.IsNullOrWhiteSpace(settings.PreprocessorAiModelId))
            {
                return provider.Models?.FirstOrDefault(x =>
                    string.Equals(x.Id, settings.PreprocessorAiModelId, StringComparison.OrdinalIgnoreCase));
            }

            return null;
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
