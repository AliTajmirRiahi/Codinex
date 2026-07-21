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
    sealed class ChatCapabilityResult
    {
        public CapabilityProbeResult SupportsStreaming { get; set; } = CapabilityProbeResult.Unsupported;

        public CapabilityProbeResult SupportsToolCalling { get; set; } = CapabilityProbeResult.Unsupported;
    }

    public sealed class ProviderCapabilityChecker : IProviderCapabilityChecker
    {
        private readonly IOpenAiCompatibleClient _client;

        public ProviderCapabilityChecker(IOpenAiCompatibleClient client)
        {
            _client = client;
        }

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

                await foreach (var chunk in _client.StreamPostAsync(
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
            try
            {
                var imageBase64 = LoadProbeImage();

                var payload = new
                {
                    model = model.Id,

                    messages = new object[]
                            {
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
                            },

                    max_tokens = 10
                };

                await _client.PostAsync(
                    provider,
                    "/chat/completions",
                    payload,
                    cancellationToken);

                return CapabilityProbeResult.Supported;
            }
            catch (OpenAiCompatibleException ex)
            {
                switch (ex.StatusCode)
                {
                    case HttpStatusCode.BadRequest:

                        if (ex.ResponseBody.Contains(
                                "image",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return CapabilityProbeResult.Unsupported;
                        }

                        return CapabilityProbeResult.Unknown;


                    case HttpStatusCode.GatewayTimeout:
                    case HttpStatusCode.ServiceUnavailable:
                    case HttpStatusCode.InternalServerError:

                        return CapabilityProbeResult.Unknown;


                    default:
                        return CapabilityProbeResult.Unknown;
                }
            }
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