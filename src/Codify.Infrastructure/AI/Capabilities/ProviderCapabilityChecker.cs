using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Infrastructure.CustomeExceptions;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Infrastructure.AI.Capabilities
{
    public sealed class ChatCapabilityResult
    {
        public CapabilityProbeResult SupportsStreaming { get; set; } = CapabilityProbeResult.Unsupported;

        public CapabilityProbeResult SupportsToolCalling { get; set; } = CapabilityProbeResult.Unsupported;
    }

    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class ProviderCapabilityChecker(IOpenAiCompatibleClient client) : IProviderCapabilityChecker
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
            var result = new ChatCapabilityResult();

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

                await foreach (var chunk in client.StreamPostAsync(
                                   provider,
                                   "/chat/completions",
                                   payload,
                                   cancellationToken))
                {
                    result.SupportsStreaming = CapabilityProbeResult.Supported;

                    if (string.IsNullOrWhiteSpace(chunk))
                        continue;

                    if (chunk == "[DONE]")
                        break;

                    var root = JObject.Parse(chunk);

                    var toolCalls =
                        root["choices"]?[0]?["delta"]?["tool_calls"];

                    if (toolCalls != null)
                        result.SupportsToolCalling = CapabilityProbeResult.Supported;
                }
            }
            catch
            {
                result.SupportsToolCalling = CapabilityProbeResult.Unknown;
                result.SupportsStreaming = CapabilityProbeResult.Unknown;
            }

            return result;
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
                "/chat/completions",
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

            const string resourceName = "Codify.Infrastructure.Resources.Images.capability-test.png";

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