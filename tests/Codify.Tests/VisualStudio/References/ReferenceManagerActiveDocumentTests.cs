using System.Threading.Tasks;

using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.TestCommon.Fakes;
using Codify.VisualStudio.References;

using FluentAssertions;

using NSubstitute;

using NUnit.Framework;

namespace Codify.Tests.VisualStudio.References;

[TestFixture]
public class ReferenceManagerActiveDocumentTests
{
    [Test]
    public async Task GetActiveDocumentAsync_Should_CallProviderOnlyOnceAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var provider = Substitute.For<IActiveDocumentProvider>();

        var expected = new ReferenceItem();

        provider
            .GetActiveDocumentAsync()
            .Returns(expected);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            new TestExecutionPipeline());

        // Act
        await sut.GetActiveDocumentAsync();
        await sut.GetActiveDocumentAsync();

        // Assert
        await provider.Received(1).GetActiveDocumentAsync();// Exact 1 time GetActiveDocumentAsync is called
    }

    [Test]
    public async Task GetActiveDocumentAsync_Should_ReturnCachedDocumentAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var provider = Substitute.For<IActiveDocumentProvider>();

        var expected = new ReferenceItem();

        provider
            .GetActiveDocumentAsync()
            .Returns(expected);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            new TestExecutionPipeline());

        // Act
        var first = await sut.GetActiveDocumentAsync();
        var second = await sut.GetActiveDocumentAsync();

        // Assert
        first.Should().BeSameAs(expected);
        second.Should().BeSameAs(expected);
        second.Should().BeSameAs(first);
    }

    [Test]
    public async Task GetActiveDocumentAsync_Should_ReturnProviderResultAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var provider = Substitute.For<IActiveDocumentProvider>();

        var expected = new ReferenceItem();

        provider
            .GetActiveDocumentAsync()
            .Returns(expected);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            new TestExecutionPipeline());

        // Act
        var result = await sut.GetActiveDocumentAsync();

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Test]
    public async Task GetActiveDocumentAsync_Should_ReturnNull_WhenProviderReturnsNullAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var provider = Substitute.For<IActiveDocumentProvider>();

        provider
            .GetActiveDocumentAsync()
            .Returns((ReferenceItem)null);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            new TestExecutionPipeline());

        // Act
        var result = await sut.GetActiveDocumentAsync();

        // Assert
        result.Should().BeNull();

        await provider.Received(1).GetActiveDocumentAsync();// Exact 1 time GetActiveDocumentAsync is called
    }
}