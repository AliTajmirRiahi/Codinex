using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Interfaces.AI;
using Codinex.Core.Interfaces.Chat;
using Codinex.Core.Models.AI;
using Codinex.Core.Models.Chat;
using Codinex.Core.Tools;
using Codinex.Core.Workspace.Prompt;
using Codinex.Infrastructure.AI.Providers;
using Codinex.Infrastructure.CustomeExceptions;
using Codinex.Infrastructure.Serialization;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Managers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.AI.Errors;

[TestFixture]
public class AiErrorMessageTests
{
    private IProviderClient _client = null!;
    private OpenAiCompatibleProvider _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<IProviderClient>();

        var providerManager = new ProviderManager(
            Substitute.For<IStorageService>(),
            new JsonSerializationService(),
            Substitute.For<IProviderModelService>(),
            Substitute.For<IProviderCapabilityChecker>());

        var provider = new AiProvider("openai", "OpenAI", "openai", "https://api.example.test/v1");
        provider.Enable();
        provider.AddModel(new AiModel("gpt-6.1-sol", "GPT 6.1 Sol", 128000, true, true));
        providerManager.Providers.Add(provider);

        var registry = Substitute.For<IAiToolRegistry>();
        registry.GetAll().Returns(new List<IAiTool>());

        var profiler = Substitute.For<IPromptProfiler>();
        profiler.Profile(Arg.Any<PromptContext>()).Returns(new PromptProfileResult());

        _sut = new OpenAiCompatibleProvider(
            new JsonSerializationService(),
            providerManager,
            registry,
            _client,
            profiler,
            Substitute.For<IPromptRecorder>());
    }

    private async Task<string> SendRejectedAsync(string body)
    {
        _client.PostAsync(Arg.Any<AiProvider>(), "/chat/completions", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new OpenAiCompatibleException(HttpStatusCode.BadRequest, body));

        return await _sut.SendAsync([new ChatMessage { Role = "user", Content = "Hello" }]);
    }

    [Test]
    public async Task Error_ShouldExplainModelIsUnsupported_WhenToolsAreRejectedWithReasoningEffort()
    {
        const string body =
            "Function tools with reasoning_effort are not supported for gpt-6.1-sol in /v1/chat/completions. " +
            "To use function tools, use /v1/responses or set reasoning_effort to 'none'.";

        var message = await SendRejectedAsync(body);

        message.Should().Contain("can't be used with Codinex yet");
        message.Should().Contain("select a different model");
        message.Should().Contain("Provider message: " + body);
    }

    [Test]
    public async Task Error_ShouldExplainModelIsUnsupported_WhenReasoningEffortNoneIsRejected()
    {
        const string body =
            "Unsupported value: 'reasoning_effort' does not support 'none' with this model. " +
            "Supported values are: 'low', 'medium', 'high', and 'xhigh'.";

        var message = await SendRejectedAsync(body);

        message.Should().Contain("can't be used with Codinex yet");
        message.Should().Contain("Provider message: " + body);
    }

    [Test]
    public async Task Error_ShouldKeepProviderMessage_WhenRejectionIsUnrelatedToReasoning()
    {
        var message = await SendRejectedAsync("""{"error":{"message":"Invalid value for 'messages'."}}""");

        message.Should().Be("Invalid value for 'messages'.");
    }
}
