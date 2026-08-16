using Codinex.Infrastructure.AI.Providers.OpenCode;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.AI.Providers;

[TestFixture]
public class OpenCodeSessionStoreTests
{
    private OpenCodeSessionStore _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new OpenCodeSessionStore();
    }

    [Test]
    public void TryGetSessionId_ShouldReturnFalse_WhenNoSessionStored()
    {
        var found = _sut.TryGetSessionId("conversation-1", out var sessionId);

        found.Should().BeFalse();
        sessionId.Should().BeNull();
    }

    [Test]
    public void SetSessionId_ThenTryGetSessionId_ShouldReturnStoredValue()
    {
        _sut.SetSessionId("conversation-1", "opencode-session-1");

        var found = _sut.TryGetSessionId("conversation-1", out var sessionId);

        found.Should().BeTrue();
        sessionId.Should().Be("opencode-session-1");
    }

    [Test]
    public void SetSessionId_ShouldOverwritePreviousMapping()
    {
        _sut.SetSessionId("conversation-1", "opencode-session-1");
        _sut.SetSessionId("conversation-1", "opencode-session-2");

        _sut.TryGetSessionId("conversation-1", out var sessionId);

        sessionId.Should().Be("opencode-session-2");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void TryGetSessionId_ShouldReturnFalse_ForBlankConversationId(string conversationId)
    {
        var found = _sut.TryGetSessionId(conversationId, out var sessionId);

        found.Should().BeFalse();
        sessionId.Should().BeNull();
    }
}
