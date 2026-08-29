using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.AI;
using Codinex.Core.Models.Chat;
using Codinex.Infrastructure.AI.Providers.OpenCode;
using Codinex.Infrastructure.CustomeExceptions;

namespace Codinex.Infrastructure.AI.Capabilities
{
    public sealed class ChatCapabilityResult
    {
        public CapabilityProbeResult SupportsStreaming { get; set; } = CapabilityProbeResult.Unsupported;

        public CapabilityProbeResult SupportsToolCalling { get; set; } = CapabilityProbeResult.Unsupported;

        public CapabilityProbeResult SupportsReasoning { get; set; } = CapabilityProbeResult.Unsupported;
    }

    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class ProviderCapabilityChecker(
        IProviderClient client,
        IJsonSerializer jsonSerializer) : IProviderCapabilityChecker
    {
        private const string OpenCodeProtocol = "opendcodefree";

        private const string AnthropicProtocol = "anthropic";

        private const string GeminiProtocol = "gemini";

        public async Task CheckAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken = default)
        {
            if (model.CapabilitiesChecked)
                return;

            if (IsOpenCodeFreeProvider(provider))
            {
                await CheckOpenCodeCapabilitiesAsync(provider, model, cancellationToken);
                return;
            }

            var chatCapabilities = await ProbeChatCapabilitiesAsync(
                provider,
                model,
                cancellationToken);

            var vision = await ProbeVisionCapabilityAsync(
                provider,
                model,
                cancellationToken);

            model.UpdateCapabilities(
                chatCapabilities.SupportsStreaming,
                chatCapabilities.SupportsToolCalling,
                vision,
                chatCapabilities.SupportsReasoning);
        }

        private static bool IsOpenCodeFreeProvider(AiProvider provider)
        {
            return string.Equals(provider.Protocol, OpenCodeProtocol, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// OpenCode has no OpenAI/Ollama-style chat endpoint to probe with synthetic requests, so
        /// capabilities are read directly from the model metadata already returned by
        /// "GET /provider" (the same endpoint model discovery uses) instead of live-probing.
        /// Streaming is always supported: OpenCode's whole chat flow is driven by its SSE event
        /// bus rather than an optional per-model streaming flag.
        /// </summary>
        private async Task CheckOpenCodeCapabilitiesAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await client.GetAsync(
                    provider,
                    provider.ModelEndPoint,
                    cancellationToken);

                var payload = jsonSerializer.Deserialize<OpenCodeProviderListResponseDto>(response);

                var upstreamModel = OpenCodeCatalog.FindModel(payload, model.Id);

                var capabilities = upstreamModel?.Capabilities;

                model.UpdateCapabilities(
                    CapabilityProbeResult.Supported,
                    ToProbeResult(capabilities?.ToolCall),
                    ToProbeResult(capabilities?.Attachment ?? capabilities?.Input?.Image),
                    ToProbeResult(capabilities?.Reasoning));
            }
            catch
            {
                model.UpdateCapabilities(
                    CapabilityProbeResult.Supported,
                    CapabilityProbeResult.Unknown,
                    CapabilityProbeResult.Unknown,
                    CapabilityProbeResult.Unknown);
            }
        }

        private static CapabilityProbeResult ToProbeResult(bool? supported)
        {
            if (supported == null)
                return CapabilityProbeResult.Unknown;

            return supported.Value
                ? CapabilityProbeResult.Supported
                : CapabilityProbeResult.Unsupported;
        }

        private async Task<ChatCapabilityResult> ProbeChatCapabilitiesAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken)
        {
            return new ChatCapabilityResult
            {
                SupportsStreaming = await ProbeStreamingCapabilityAsync(
                    provider,
                    model,
                    cancellationToken),

                SupportsToolCalling = await ProbeToolCallingCapabilityAsync(
                    provider,
                    model,
                    cancellationToken),

                SupportsReasoning = await ProbeReasoningCapabilityAsync(
                    provider,
                    model,
                    cancellationToken)
            };
        }

        private async Task<CapabilityProbeResult> ProbeStreamingCapabilityAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = BuildStreamingProbePayload(provider, model);

                await foreach (var chunk in client.StreamPostAsync(
                                   provider,
                                   GetChatEndpoint(provider, model, stream: true),
                                   payload,
                                   cancellationToken))
                {
                    if (string.IsNullOrWhiteSpace(chunk))
                        continue;

                    return CapabilityProbeResult.Supported;
                }

                return CapabilityProbeResult.Unsupported;
            }
            catch
            {
                return CapabilityProbeResult.Unknown;
            }
        }

        private async Task<CapabilityProbeResult> ProbeToolCallingCapabilityAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = BuildToolCallingProbePayload(provider, model);

                var response = await client.PostAsync(
                    provider,
                    GetChatEndpoint(provider, model),
                    payload,
                    cancellationToken);

                var root = JObject.Parse(response);

                return HasToolCalls(provider, root)
                    ? CapabilityProbeResult.Supported
                    : CapabilityProbeResult.Unsupported;
            }
            catch (OpenAiCompatibleException ex)
            {
                return IsToolCallingUnsupported(ex.ResponseBody)
                    ? CapabilityProbeResult.Unsupported
                    : CapabilityProbeResult.Unknown;
            }
            catch
            {
                return CapabilityProbeResult.Unknown;
            }
        }

        private async Task<CapabilityProbeResult> ProbeReasoningCapabilityAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = BuildReasoningProbePayload(provider, model);

                var response = await client.PostAsync(
                    provider,
                    GetChatEndpoint(provider, model),
                    payload,
                    cancellationToken);

                var root = JObject.Parse(response);

                return HasReasoningContent(provider, root)
                    ? CapabilityProbeResult.Supported
                    : CapabilityProbeResult.Unsupported;
            }
            catch (OpenAiCompatibleException ex)
            {
                return IsReasoningUnsupported(ex.ResponseBody)
                    ? CapabilityProbeResult.Unsupported
                    : CapabilityProbeResult.Unknown;
            }
            catch
            {
                return CapabilityProbeResult.Unknown;
            }
        }

        private async Task<CapabilityProbeResult> ProbeVisionCapabilityAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken)
        {
            try
            {
                var imageBase64 = LoadProbeImage();

                await PostVisionProbeAsync(
                    provider,
                    model,
                    imageBase64,
                    useMaxCompletionTokens: false,
                    cancellationToken);

                return CapabilityProbeResult.Supported;
            }
            catch (OpenAiCompatibleException ex)
                when (ex.StatusCode == HttpStatusCode.BadRequest && IsMaxTokensUnsupported(ex.ResponseBody))
            {
                try
                {
                    var imageBase64 = LoadProbeImage();

                    await PostVisionProbeAsync(
                        provider,
                        model,
                        imageBase64,
                        useMaxCompletionTokens: true,
                        cancellationToken);

                    return CapabilityProbeResult.Supported;
                }
                catch (OpenAiCompatibleException retryEx)
                {
                    return MapVisionProbeException(retryEx);
                }
            }
            catch (OpenAiCompatibleException ex)
            {
                return MapVisionProbeException(ex);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return CapabilityProbeResult.Unknown;
            }
        }

        private static string GetChatEndpoint(AiProvider provider, AiModel model, bool stream = false)
        {
            if (IsAnthropicProvider(provider))
                return "/messages";

            if (IsGeminiProvider(provider))
                return stream
                    ? $"/models/{model.Id}:streamGenerateContent?alt=sse"
                    : $"/models/{model.Id}:generateContent";

            return provider.Protocol == "openai" ? "/chat/completions" : "/api/chat";
        }

        private static bool HasToolCalls(AiProvider provider, JObject root)
        {
            if (IsAnthropicProvider(provider))
            {
                return root["content"] is JArray content
                       && content.Any(x => x["type"]?.ToString() == "tool_use");
            }

            if (IsGeminiProvider(provider))
            {
                return root["candidates"]?[0]?["content"]?["parts"] is JArray geminiParts
                       && geminiParts.Any(x => x["functionCall"] != null);
            }

            var toolCalls = provider.Protocol == "openai"
                ? root["choices"]?[0]?["message"]?["tool_calls"]
                : root["message"]?["tool_calls"];

            return toolCalls != null;
        }

        private async Task PostVisionProbeAsync(
            AiProvider provider,
            AiModel model,
            string imageBase64,
            bool useMaxCompletionTokens,
            CancellationToken cancellationToken)
        {
            var payload = BuildVisionProbePayload(
                provider,
                model,
                imageBase64,
                useMaxCompletionTokens);

            await client.PostAsync(
                provider,
                GetChatEndpoint(provider, model),
                payload,
                cancellationToken);
        }

        private static object BuildStreamingProbePayload(AiProvider provider, AiModel model)
        {
            if (IsGeminiProvider(provider))
            {
                return new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = "Reply with pong." } }
                        }
                    },
                    generationConfig = new { maxOutputTokens = 10 }
                };
            }

            if (IsAnthropicProvider(provider))
            {
                return new
                {
                    model = model.Id,

                    max_tokens = 10,

                    stream = true,

                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = "Reply with pong."
                        }
                    }
                };
            }

            return new
            {
                model = model.Id,

                stream = true,

                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = "Reply with pong."
                    }
                }
            };
        }

        private static object BuildToolCallingProbePayload(AiProvider provider, AiModel model)
        {
            if (IsGeminiProvider(provider))
            {
                return new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = "Use the ping tool." } }
                        }
                    },
                    tools = new[]
                    {
                        new
                        {
                            functionDeclarations = new[]
                            {
                                new
                                {
                                    name = "ping",
                                    description = "Returns pong.",
                                    parameters = new { type = "object", properties = new { } }
                                }
                            }
                        }
                    },
                    toolConfig = new
                    {
                        functionCallingConfig = new { mode = "ANY" }
                    },
                    generationConfig = new { maxOutputTokens = 32 }
                };
            }

            if (IsAnthropicProvider(provider))
            {
                return new
                {
                    model = model.Id,

                    max_tokens = 32,

                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = "Use the ping tool."
                        }
                    },

                    tools = new[]
                    {
                        new
                        {
                            name = "ping",

                            description = "Returns pong.",

                            input_schema = new
                            {
                                type = "object",

                                properties = new { }
                            }
                        }
                    },

                    tool_choice = new
                    {
                        type = "tool",
                        name = "ping"
                    }
                };
            }

            return new
            {
                model = model.Id,

                stream = false,

                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = "Use the ping tool."
                    }
                },

                tools = new[]
                {
                    new
                    {
                        type = "function",

                        function = new
                        {
                            name = "ping",

                            description = "Returns pong.",

                            parameters = new
                            {
                                type = "object",

                                properties = new { }
                            }
                        }
                    }
                }
            };
        }

        private static object BuildReasoningProbePayload(AiProvider provider, AiModel model)
        {
            if (IsGeminiProvider(provider))
            {
                return new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = "What is 2 + 2? Think step by step." } }
                        }
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = 1056,
                        thinkingConfig = new { thinkingBudget = 1024, includeThoughts = true }
                    }
                };
            }

            if (IsAnthropicProvider(provider))
            {
                return new
                {
                    model = model.Id,

                    max_tokens = 1056,

                    thinking = new
                    {
                        type = "enabled",
                        budget_tokens = 1024
                    },

                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = "What is 2 + 2? Think step by step."
                        }
                    }
                };
            }

            return new
            {
                model = model.Id,

                stream = false,

                reasoning = new { effort = "low" },

                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = "What is 2 + 2? Think step by step."
                    }
                }
            };
        }

        private static bool HasReasoningContent(AiProvider provider, JObject root)
        {
            if (IsAnthropicProvider(provider))
            {
                return root["content"] is JArray content
                       && content.Any(x => x["type"]?.ToString() == "thinking");
            }

            if (IsGeminiProvider(provider))
            {
                return root["candidates"]?[0]?["content"]?["parts"] is JArray geminiParts
                       && geminiParts.Any(x => x["thought"]?.Value<bool>() == true);
            }

            var message = provider.Protocol == "openai"
                ? root["choices"]?[0]?["message"]
                : root["message"];

            return !string.IsNullOrWhiteSpace(message?["reasoning_content"]?.ToString())
                   || !string.IsNullOrWhiteSpace(message?["reasoning"]?.ToString())
                   || !string.IsNullOrWhiteSpace(message?["thinking"]?.ToString());
        }

        private static bool IsReasoningUnsupported(string responseBody)
        {
            return (ContainsIgnoreCase(responseBody, "reasoning") || ContainsIgnoreCase(responseBody, "thinking"))
                   && (ContainsIgnoreCase(responseBody, "unsupported parameter")
                       || ContainsIgnoreCase(responseBody, "unknown parameter")
                       || ContainsIgnoreCase(responseBody, "not supported")
                       || ContainsIgnoreCase(responseBody, "does not support")
                       || ContainsIgnoreCase(responseBody, "doesn't support"));
        }

        private static object BuildVisionProbePayload(
            AiProvider provider,
            AiModel model,
            string imageBase64,
            bool useMaxCompletionTokens)
        {
            if (IsGeminiProvider(provider))
            {
                return new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { text = "Describe this image." },
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = "image/png",
                                        data = imageBase64
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new { maxOutputTokens = 10 }
                };
            }

            if (IsAnthropicProvider(provider))
            {
                return new
                {
                    model = model.Id,

                    messages = CreateAnthropicVisionProbeMessages(imageBase64),

                    max_tokens = 10
                };
            }

            if (useMaxCompletionTokens)
            {
                return new
                {
                    model = model.Id,

                    messages = CreateVisionProbeMessages(imageBase64),

                    max_completion_tokens = 10
                };
            }

            return new
            {
                model = model.Id,

                messages = CreateVisionProbeMessages(imageBase64),

                max_tokens = 10
            };
        }

        private static object[] CreateAnthropicVisionProbeMessages(string imageBase64)
        {
            return
            [
                new
                {
                    role = "user",

                    content = new object[]
                    {
                        new
                        {
                            type = "text",

                            text = "Describe this image."
                        },

                        new
                        {
                            type = "image",

                            source = new
                            {
                                type = "base64",
                                media_type = "image/png",
                                data = imageBase64
                            }
                        }
                    }
                }
            ];
        }

        private static bool IsAnthropicProvider(AiProvider provider)
        {
            return string.Equals(provider.Protocol, AnthropicProtocol, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(provider.Id, "anthropic", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGeminiProvider(AiProvider provider)
        {
            return string.Equals(provider.Protocol, GeminiProtocol, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(provider.Id, "gemini", StringComparison.OrdinalIgnoreCase);
        }

        private static object[] CreateVisionProbeMessages(string imageBase64)
        {
            return
            [
                new
                {
                    role = "user",

                    content = new object[]
                    {
                        new
                        {
                            type = "text",

                            text = "Describe this image."
                        },

                        new
                        {
                            type = "image_url",

                            image_url = new
                            {
                                url = $"data:image/png;base64,{imageBase64}"
                            }
                        }
                    }
                }
            ];
        }

        private static CapabilityProbeResult MapVisionProbeException(OpenAiCompatibleException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.BadRequest:

                    if (IsVisionUnsupported(ex.ResponseBody))
                        return CapabilityProbeResult.Unsupported;

                    return CapabilityProbeResult.Unknown;


                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.InternalServerError:

                    return CapabilityProbeResult.Unknown;


                default:
                    return CapabilityProbeResult.Unknown;
            }
        }

        private static bool IsMaxTokensUnsupported(string responseBody)
        {
            return ContainsIgnoreCase(responseBody, "max_tokens")
                   && (ContainsIgnoreCase(responseBody, "max_completion_tokens")
                       || ContainsIgnoreCase(responseBody, "unsupported parameter")
                       || ContainsIgnoreCase(responseBody, "not supported"));
        }

        private static bool IsToolCallingUnsupported(string responseBody)
        {
            return ContainsIgnoreCase(responseBody, "does not support tools")
                   || ContainsIgnoreCase(responseBody, "doesn't support tools")
                   || ContainsIgnoreCase(responseBody, "tools are not supported")
                   || ContainsIgnoreCase(responseBody, "tool calling is not supported")
                   || ContainsIgnoreCase(responseBody, "does not support tool calling")
                   || ContainsIgnoreCase(responseBody, "doesn't support tool calling");
        }

        private static bool IsVisionUnsupported(string responseBody)
        {
            return ContainsIgnoreCase(responseBody, "does not support image")
                   || ContainsIgnoreCase(responseBody, "doesn't support image")
                   || ContainsIgnoreCase(responseBody, "image input is not supported")
                   || ContainsIgnoreCase(responseBody, "images are not supported")
                   || ContainsIgnoreCase(responseBody, "vision is not supported")
                   || ContainsIgnoreCase(responseBody, "does not support vision")
                   || ContainsIgnoreCase(responseBody, "doesn't support vision")
                   || ContainsIgnoreCase(responseBody, "not a vision model")
                   || ContainsIgnoreCase(responseBody, "only supports text");
        }

        private static bool ContainsIgnoreCase(string value, string text)
        {
            return value?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string LoadProbeImage()
        {
            var assembly = typeof(ProviderCapabilityChecker).Assembly;

            const string resourceName = "Codinex.Infrastructure.Resources.Images.capability-test.png";

            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                throw new InvalidOperationException(
                    $"Embedded resource not found: {resourceName}");

            using var memoryStream = new MemoryStream();

            stream.CopyTo(memoryStream);

            return Convert.ToBase64String(
                memoryStream.ToArray());
        }
    }
}