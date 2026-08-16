using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.AI.Capabilities;
using Codinex.Infrastructure.Serialization;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.AI.Providers;

[TestFixture]
public class OpenCodeProviderCapabilityCheckerTests
{
    private IProviderClient _client = null!;
    private ProviderCapabilityChecker _sut = null!;
    private AiProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<IProviderClient>();
        _sut = new ProviderCapabilityChecker(_client, new JsonSerializationService());

        _provider = Newtonsoft.Json.JsonConvert.DeserializeObject<AiProvider>("""
        {
          "Id": "opendcodefree",
          "Name": "OpenCode.ai (Free Models)",
          "Protocol": "opendcodefree",
          "BaseUrl": "http://127.0.0.1:4096",
          "ModelEndPoint": "/provider",
          "NeedApiKey": false,
          "IsLocal": false,
          "IsEnabled": false
        }
        """)!;
    }

    private static AiModel Model(string id = "deepseek-v4-flash-free")
    {
        return new AiModel(id, id, 128000, true, true);
    }

    [Test]
    public async Task CheckAsync_ShouldDeriveCapabilitiesFromProviderMetadata_WithoutProbingAnyEndpoint()
    {
        const string response = """
        {
          "all": [
            {
              "id": "opencode",
              "models": {
                "deepseek-v4-flash-free": {
                  "id": "deepseek-v4-flash-free",
                  "cost": { "input": 0, "output": 0 },
                  "capabilities": {
                    "reasoning": true,
                    "attachment": true,
                    "toolcall": true,
                    "input": { "text": true, "image": true }
                  }
                }
              }
            }
          ]
        }
        """;

        _client.GetAsync(_provider, "/provider", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var model = Model();

        await _sut.CheckAsync(_provider, model);

        model.CapabilitiesChecked.Should().BeTrue();
        model.SupportsStreaming.Should().Be(CapabilityProbeResult.Supported);
        model.SupportsToolCalling.Should().Be(CapabilityProbeResult.Supported);
        model.SupportsVision.Should().Be(CapabilityProbeResult.Supported);
        model.SupportsReasoning.Should().Be(CapabilityProbeResult.Supported);

        // Only the metadata lookup should have been made — no live probe requests.
        await _client.DidNotReceive().PostAsync(Arg.Any<AiProvider>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
        _client.DidNotReceive().StreamPostAsync(Arg.Any<AiProvider>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CheckAsync_ShouldReportUnsupported_WhenCapabilitiesAreFalse()
    {
        const string response = """
        {
          "all": [
            {
              "id": "opencode",
              "models": {
                "text-only-free": {
                  "id": "text-only-free",
                  "cost": { "input": 0, "output": 0 },
                  "capabilities": {
                    "reasoning": false,
                    "attachment": false,
                    "toolcall": false
                  }
                }
              }
            }
          ]
        }
        """;

        _client.GetAsync(_provider, "/provider", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var model = Model("text-only-free");

        await _sut.CheckAsync(_provider, model);

        model.SupportsStreaming.Should().Be(CapabilityProbeResult.Supported);
        model.SupportsToolCalling.Should().Be(CapabilityProbeResult.Unsupported);
        model.SupportsVision.Should().Be(CapabilityProbeResult.Unsupported);
        model.SupportsReasoning.Should().Be(CapabilityProbeResult.Unsupported);
    }

    [Test]
    public async Task CheckAsync_ShouldReportUnknownNonStreaming_WhenModelMetadataIsMissing()
    {
        const string response = """{ "all": [] }""";

        _client.GetAsync(_provider, "/provider", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var model = Model("some-model-not-in-catalog");

        await _sut.CheckAsync(_provider, model);

        // Streaming is always supported for OpenCode regardless of metadata availability.
        model.SupportsStreaming.Should().Be(CapabilityProbeResult.Supported);
        model.SupportsToolCalling.Should().Be(CapabilityProbeResult.Unknown);
        model.SupportsVision.Should().Be(CapabilityProbeResult.Unknown);
        model.SupportsReasoning.Should().Be(CapabilityProbeResult.Unknown);
    }

    [Test]
    public async Task CheckAsync_ShouldReportUnknownNonStreaming_WhenServerIsUnavailable()
    {
        _client.GetAsync(_provider, "/provider", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new System.Net.Http.HttpRequestException("connection refused")));

        var model = Model();

        await _sut.CheckAsync(_provider, model);

        model.CapabilitiesChecked.Should().BeTrue();
        model.SupportsStreaming.Should().Be(CapabilityProbeResult.Supported);
        model.SupportsToolCalling.Should().Be(CapabilityProbeResult.Unknown);
        model.SupportsVision.Should().Be(CapabilityProbeResult.Unknown);
        model.SupportsReasoning.Should().Be(CapabilityProbeResult.Unknown);
    }

    [Test]
    public async Task CheckAsync_ShouldFallBackToInputImage_WhenAttachmentFlagIsAbsent()
    {
        const string response = """
        {
          "all": [
            {
              "id": "opencode",
              "models": {
                "vision-via-input-free": {
                  "id": "vision-via-input-free",
                  "cost": { "input": 0, "output": 0 },
                  "capabilities": {
                    "input": { "text": true, "image": true }
                  }
                }
              }
            }
          ]
        }
        """;

        _client.GetAsync(_provider, "/provider", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var model = Model("vision-via-input-free");

        await _sut.CheckAsync(_provider, model);

        model.SupportsVision.Should().Be(CapabilityProbeResult.Supported);
    }

    [Test]
    public async Task CheckAsync_ShouldNoOp_WhenCapabilitiesAlreadyChecked()
    {
        var model = Model();
        model.UpdateCapabilities(
            CapabilityProbeResult.Supported,
            CapabilityProbeResult.Supported,
            CapabilityProbeResult.Supported,
            CapabilityProbeResult.Supported);

        await _sut.CheckAsync(_provider, model);

        await _client.DidNotReceive().GetAsync(Arg.Any<AiProvider>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
