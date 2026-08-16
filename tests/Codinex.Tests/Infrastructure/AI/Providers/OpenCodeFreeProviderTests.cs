using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.Infrastructure.AI.Providers;
using Codinex.Infrastructure.AI.Providers.OpenCode;
using Codinex.Infrastructure.Chat;
using Codinex.Infrastructure.CustomeExceptions;
using Codinex.Infrastructure.Serialization;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Managers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.AI.Providers;

[TestFixture]
public class OpenCodeFreeProviderTests
{
    private IProviderClient _client = null!;
    private IOpenCodeSessionStore _sessionStore = null!;
    private ProviderManager _providerManager = null!;
    private ChatSessionService _chatSessionService = null!;
    private AiProvider _provider = null!;
    private AiModel _model = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<IProviderClient>();
        _sessionStore = Substitute.For<IOpenCodeSessionStore>();

        _providerManager = new ProviderManager(
            Substitute.For<IStorageService>(),
            new JsonSerializationService(),
            Substitute.For<IProviderModelService>(),
            Substitute.For<IProviderCapabilityChecker>());

        _provider = new AiProvider("opendcodefree", "OpenCode (Free Models)", "opendcodefree", "http://127.0.0.1:4096");
        _provider.Enable();

        _model = new AiModel("deepseek-v4-flash-free", "DeepSeek V4 Flash (Free)", 128000, true, true);
        _provider.AddModel(_model);

        _providerManager.Providers.Add(_provider);

        // No active chat session wired up in these unit tests: the provider falls back to
        // creating a fresh OpenCode session per call. Session reuse itself is covered by
        // OpenCodeSessionStoreTests.
        _chatSessionService = new ChatSessionService(null!, _providerManager, null!);
    }

    private OpenCodeFreeProvider CreateSut()
    {
        return new OpenCodeFreeProvider(
            new JsonSerializationService(),
            _providerManager,
            _client,
            _sessionStore,
            _chatSessionService);
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(
        IEnumerable<string> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return item;
        }
    }

    private void SetUpSession(string sessionId)
    {
        _client.PostAsync(_provider, "/session", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult($$"""{"id":"{{sessionId}}"}"""));

        _client.PostAsync(_provider, Arg.Is<string>(e => e.EndsWith("/message")), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{}"));
    }

    private void SetUpEvents(params string[] rawEvents)
    {
        _client.StreamGetAsync(_provider, "/event", Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(rawEvents));
    }

    private static List<ChatMessage> Messages(string userText)
    {
        return
        [
            new ChatMessage { Role = "user", Content = userText }
        ];
    }

    [Test]
    public async Task SendStreamAsync_ShouldEmitTextDeltasThenCompleted_ForHappyPath()
    {
        SetUpSession("sess-1");
        SetUpEvents(
            """{"type":"server.connected"}""",
            """{"type":"message.part.updated","properties":{"sessionID":"sess-1","part":{"type":"text"},"delta":"Hel"}}""",
            """{"type":"message.part.updated","properties":{"sessionID":"sess-1","part":{"type":"text"},"delta":"lo"}}""",
            """{"type":"session.idle","properties":{"sessionID":"sess-1"}}""");

        var sut = CreateSut();

        var events = await sut.SendStreamAsync(Messages("Hi")).ToListAsync();

        events.Select(e => e.Type).Should().Equal(
            ConversationEventType.TextDelta,
            ConversationEventType.TextDelta,
            ConversationEventType.ConversationCompleted);

        events[0].Payload!.ToString().Should().Be("Hel");
        events[1].Payload!.ToString().Should().Be("lo");
    }

    [Test]
    public async Task SendStreamAsync_ShouldIgnoreEventsForOtherSessions()
    {
        SetUpSession("sess-1");
        SetUpEvents(
            """{"type":"message.part.updated","properties":{"sessionID":"other-session","part":{"type":"text"},"delta":"nope"}}""",
            """{"type":"message.part.updated","properties":{"sessionID":"sess-1","part":{"type":"text"},"delta":"yes"}}""",
            """{"type":"session.idle","properties":{"sessionID":"sess-1"}}""");

        var sut = CreateSut();

        var events = await sut.SendStreamAsync(Messages("Hi")).ToListAsync();

        var deltas = events.Where(e => e.Type == ConversationEventType.TextDelta).ToList();
        deltas.Should().ContainSingle();
        deltas[0].Payload!.ToString().Should().Be("yes");
    }

    [Test]
    public async Task SendStreamAsync_ShouldSkipMalformedEvents_AndContinueProcessing()
    {
        SetUpSession("sess-1");
        SetUpEvents(
            "not-json-at-all {{{",
            """{"type":"message.part.updated","properties":{"sessionID":"sess-1","part":{"type":"text"},"delta":"ok"}}""",
            """{"type":"session.idle","properties":{"sessionID":"sess-1"}}""");

        var sut = CreateSut();

        var events = await sut.SendStreamAsync(Messages("Hi")).ToListAsync();

        events.Should().Contain(e => e.Type == ConversationEventType.TextDelta && e.Payload!.ToString() == "ok");
        events.Last().Type.Should().Be(ConversationEventType.ConversationCompleted);
    }

    [Test]
    public async Task SendStreamAsync_ShouldYieldFailedEvent_OnSessionError()
    {
        SetUpSession("sess-1");
        SetUpEvents(
            """{"type":"session.error","properties":{"sessionID":"sess-1","error":{"name":"ProviderAuthError","data":{"message":"bad key"}}}}""");

        var sut = CreateSut();

        var events = await sut.SendStreamAsync(Messages("Hi")).ToListAsync();

        events.Should().ContainSingle();
        events[0].Type.Should().Be(ConversationEventType.ConversationFailed);
        events[0].DisplayMessage.Should().Be("bad key");
    }

    [Test]
    public async Task SendStreamAsync_ShouldYieldFailedEvent_WhenServerUnavailable()
    {
        // The very first call the provider makes is session creation (POST /session), so a
        // connection failure there stands in for "the OpenCode server is not running".
        _client.PostAsync(_provider, "/session", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new System.Net.Http.HttpRequestException("connection refused")));

        var sut = CreateSut();

        var events = await sut.SendStreamAsync(Messages("Hi")).ToListAsync();

        events.Should().ContainSingle();
        events[0].Type.Should().Be(ConversationEventType.ConversationFailed);
    }

    [Test]
    public async Task SendStreamAsync_ShouldRecreateSession_WhenCachedSessionIsInvalid()
    {
        // No active chat session in this test, so GetOrCreateSessionIdAsync always creates a
        // fresh session; this exercises the "message send returns 404" recovery path directly.
        _client.PostAsync(_provider, "/session", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult("""{"id":"sess-1"}"""),
                Task.FromResult("""{"id":"sess-2"}"""));

        _client.PostAsync(_provider, "/session/sess-1/message", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new OpenAiCompatibleException(HttpStatusCode.NotFound, "session not found")));

        _client.PostAsync(_provider, "/session/sess-2/message", Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{}"));

        _client.StreamGetAsync(_provider, "/event", Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(
            [
                """{"type":"message.part.updated","properties":{"sessionID":"sess-2","part":{"type":"text"},"delta":"recovered"}}""",
                """{"type":"session.idle","properties":{"sessionID":"sess-2"}}"""
            ]));

        var sut = CreateSut();

        var events = await sut.SendStreamAsync(Messages("Hi")).ToListAsync();

        events.Should().Contain(e => e.Type == ConversationEventType.TextDelta && e.Payload!.ToString() == "recovered");
        events.Last().Type.Should().Be(ConversationEventType.ConversationCompleted);
    }
}

internal static class AsyncEnumerableTestExtensions
{
    public static async Task<List<ConversationEvent>> ToListAsync(this IAsyncEnumerable<ConversationEvent> source)
    {
        var results = new List<ConversationEvent>();

        await foreach (var item in source)
        {
            results.Add(item);
        }

        return results;
    }
}
