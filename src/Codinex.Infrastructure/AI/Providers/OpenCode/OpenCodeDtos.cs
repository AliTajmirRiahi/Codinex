using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codinex.Infrastructure.AI.Providers.OpenCode
{
    /// <summary>
    /// Typed DTOs for the OpenCode Server API (https://opencode.ai/docs/server/).
    /// These are intentionally kept internal to the OpenCode provider integration and must not
    /// leak outside this namespace.
    /// </summary>
    internal sealed class OpenCodeProviderListResponseDto
    {
        public List<OpenCodeProviderDto> All { get; set; }
    }

    internal sealed class OpenCodeProviderDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public Dictionary<string, OpenCodeModelDto> Models { get; set; }
    }

    internal sealed class OpenCodeModelDto
    {
        public string Id { get; set; }

        public string ProviderID { get; set; }

        public string Name { get; set; }

        public OpenCodeModelCostDto Cost { get; set; }

        public OpenCodeModelLimitDto Limit { get; set; }

        public OpenCodeModelCapabilitiesDto Capabilities { get; set; }
    }

    internal sealed class OpenCodeModelCostDto
    {
        public double Input { get; set; }

        public double Output { get; set; }
    }

    internal sealed class OpenCodeModelLimitDto
    {
        public int Context { get; set; }

        public int Output { get; set; }
    }

    /// <summary>
    /// Mirrors OpenCode's Model.capabilities shape. Used by ProviderCapabilityChecker to derive
    /// streaming/tool-calling/vision/reasoning support directly from provider metadata instead of
    /// live-probing a chat endpoint that doesn't exist for OpenCode's session-based protocol.
    /// </summary>
    internal sealed class OpenCodeModelCapabilitiesDto
    {
        public bool? Temperature { get; set; }

        public bool? Reasoning { get; set; }

        public bool? Attachment { get; set; }

        [JsonProperty("toolcall")]
        public bool? ToolCall { get; set; }

        public OpenCodeModelIoDto Input { get; set; }

        public OpenCodeModelIoDto Output { get; set; }
    }

    internal sealed class OpenCodeModelIoDto
    {
        public bool? Text { get; set; }

        public bool? Audio { get; set; }

        public bool? Image { get; set; }

        public bool? Video { get; set; }

        public bool? Pdf { get; set; }
    }

    internal sealed class OpenCodeSessionDto
    {
        public string Id { get; set; }
    }

    /// <summary>
    /// Shape of every event on the OpenCode "GET /event" bus. "Properties" is a flattened
    /// superset of the fields used by the event types this provider cares about
    /// (message.part.updated, message.updated, session.error, session.idle, server.connected).
    /// </summary>
    internal sealed class OpenCodeEventDto
    {
        public string Type { get; set; }

        public OpenCodeEventPropertiesDto Properties { get; set; }
    }

    internal sealed class OpenCodeEventPropertiesDto
    {
        public string SessionID { get; set; }

        public string MessageID { get; set; }

        public OpenCodePartDto Part { get; set; }

        public string Delta { get; set; }

        public OpenCodeErrorDto Error { get; set; }

        public OpenCodeMessageInfoDto Info { get; set; }
    }

    internal sealed class OpenCodePartDto
    {
        public string Id { get; set; }

        public string SessionID { get; set; }

        public string MessageID { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }
    }

    internal sealed class OpenCodeMessageInfoDto
    {
        public string Id { get; set; }

        public string SessionID { get; set; }

        public string Role { get; set; }

        public string Finish { get; set; }

        public OpenCodeErrorDto Error { get; set; }
    }

    internal sealed class OpenCodeErrorDto
    {
        public string Name { get; set; }

        public OpenCodeErrorDataDto Data { get; set; }
    }

    internal sealed class OpenCodeErrorDataDto
    {
        public string Message { get; set; }

        public string ProviderID { get; set; }

        public int? StatusCode { get; set; }

        public bool? IsRetryable { get; set; }
    }
}
