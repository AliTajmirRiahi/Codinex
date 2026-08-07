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
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.CustomeExceptions;

namespace Codinex.Infrastructure.AI.Capabilities
{
    public sealed class ChatCapabilityResult
    {
        public CapabilityProbeResult SupportsStreaming { get; set; } = CapabilityProbeResult.Unsupported;

        public CapabilityProbeResult SupportsToolCalling { get; set; } = CapabilityProbeResult.Unsupported;
    }

    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class ProviderCapabilityChecker(IProviderClient client) : IProviderCapabilityChecker
    {
        public async Task CheckAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken = default)
        {
            if (model.CapabilitiesChecked)
                return;

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
                CapabilityProbeResult.Unsupported);
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
                var payload = new
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

                await foreach (var chunk in client.StreamPostAsync(
                                   provider,
                                   GetChatEndpoint(provider),
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
                var payload = new
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

                var response = await client.PostAsync(
                    provider,
                    GetChatEndpoint(provider),
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

        private async Task<CapabilityProbeResult> ProbeVisionCapabilityAsync(
            AiProvider provider,
            AiModel model,
            CancellationToken cancellationToken)
        {
            var imageBase64 = LoadProbeImage();

            try
            {
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
        }

        private static string GetChatEndpoint(AiProvider provider)
        {
            return provider.Protocol == "openai" ? "/chat/completions" : "/api/chat";
        }

        private static bool HasToolCalls(AiProvider provider, JObject root)
        {
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
            object payload;

            if (useMaxCompletionTokens)
            {
                payload = new
                {
                    model = model.Id,

                    messages = CreateVisionProbeMessages(imageBase64),

                    max_completion_tokens = 10
                };
            }
            else
            {
                payload = new
                {
                    model = model.Id,

                    messages = CreateVisionProbeMessages(imageBase64),

                    max_tokens = 10
                };
            }

            await client.PostAsync(
                provider,
                GetChatEndpoint(provider),
                payload,
                cancellationToken);
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