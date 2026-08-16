using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.ModelManagement.Retrievers;
using Codinex.Infrastructure.Serialization;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.AI.Providers;

[TestFixture]
public class OpenCodeFreeModelRetrieverTests
{
    private IProviderClient _client = null!;
    private IJsonSerializer _serializer = null!;
    private OpenCodeFreeModelRetriever _sut = null!;
    private AiProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<IProviderClient>();
        _serializer = new JsonSerializationService();
        _sut = new OpenCodeFreeModelRetriever(_client, _serializer);

        // Deserialize (rather than use the convenience constructor) so ModelEndPoint matches the
        // "/provider" value actually shipped in providers.json for this provider.
        _provider = Newtonsoft.Json.JsonConvert.DeserializeObject<AiProvider>("""
        {
          "Id": "opendcodefree",
          "Name": "OpenCode (Free Models)",
          "Protocol": "opendcodefree",
          "BaseUrl": "http://127.0.0.1:4096",
          "ModelEndPoint": "/provider",
          "NeedApiKey": false,
          "IsLocal": false,
          "IsEnabled": false
        }
        """)!;
    }

    [Test]
    public void CanHandle_ShouldReturnTrue_ForOpendcodefreeProtocol()
    {
        _sut.CanHandle(_provider).Should().BeTrue();
    }

    [Test]
    public void CanHandle_ShouldReturnFalse_ForOtherProtocols()
    {
        var openAiProvider = new AiProvider("openAI", "OpenAI", "openai", "https://api.openai.com/v1");

        _sut.CanHandle(openAiProvider).Should().BeFalse();
    }

    [Test]
    public async Task GetModelsAsync_ShouldReturnOnlyFreeModelsFromOpencodeProvider()
    {
        const string response = """
        {
          "all": [
            {
              "id": "opencode",
              "models": {
                "deepseek-v4-flash-free": {
                  "id": "deepseek-v4-flash-free",
                  "name": "DeepSeek V4 Flash (Free)",
                  "cost": { "input": 0, "output": 0 },
                  "limit": { "context": 128000, "output": 8000 }
                },
                "paid-model": {
                  "id": "paid-model",
                  "name": "Paid Model",
                  "cost": { "input": 1.5, "output": 3 },
                  "limit": { "context": 200000, "output": 8000 }
                }
              }
            },
            {
              "id": "anthropic",
              "models": {
                "claude-something": {
                  "id": "claude-something",
                  "name": "Claude",
                  "cost": { "input": 0, "output": 0 }
                }
              }
            }
          ],
          "default": {},
          "connected": ["opencode"]
        }
        """;

        _client.GetAsync(_provider, "/provider", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var models = await _sut.GetModelsAsync(_provider);

        models.Should().ContainSingle();
        var model = models[0];
        model.Id.Should().Be("deepseek-v4-flash-free");
        model.Name.Should().Be("DeepSeek V4 Flash (Free)");
        model.TokenLimit.Should().Be(128000);
    }

    [Test]
    public async Task GetModelsAsync_ShouldReturnEmpty_WhenOpencodeProviderIsMissing()
    {
        const string response = """{ "all": [], "default": {}, "connected": [] }""";

        _client.GetAsync(_provider, "/provider", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var models = await _sut.GetModelsAsync(_provider);

        models.Should().BeEmpty();
    }
}
