using System.Threading.Tasks;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;
using Codinex.TestCommon.Fakes;
using Codinex.VisualStudio.References;
using FluentAssertions;

using NSubstitute;

using NUnit.Framework;

namespace Codinex.Tests.VisualStudio.References;

[TestFixture]
public class ReferenceManagerActiveDocumentTests
{
    [Test]
    public async Task GetActiveDocumentAsync_Should_CallProviderOnlyOnceAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var provider = Substitute.For<IActiveDocumentProvider>();
        var errorHandler = Substitute.For<IErrorHandler>();

        var expected = new ReferenceItem();

        provider
            .GetActiveDocumentAsync()
            .Returns(expected);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            Substitute.For<IWorkspaceFileWatcher>(),
            Substitute.For<IFileReferenceBuilder>(),
            Substitute.For<ISymbolReferenceWatcher>(),
            new TestExecutionPipeline(),
            errorHandler);

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
        var errorHandler = Substitute.For<IErrorHandler>();

        var expected = new ReferenceItem();

        provider
            .GetActiveDocumentAsync()
            .Returns(expected);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            Substitute.For<IWorkspaceFileWatcher>(),
            Substitute.For<IFileReferenceBuilder>(),
            Substitute.For<ISymbolReferenceWatcher>(),
            new TestExecutionPipeline(),
            errorHandler);

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
        var errorHandler = Substitute.For<IErrorHandler>();

        var expected = new ReferenceItem();

        provider
            .GetActiveDocumentAsync()
            .Returns(expected);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            Substitute.For<IWorkspaceFileWatcher>(),
            Substitute.For<IFileReferenceBuilder>(),
            Substitute.For<ISymbolReferenceWatcher>(),
            new TestExecutionPipeline(),
            errorHandler);

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
        var errorHandler = Substitute.For<IErrorHandler>();

        provider
            .GetActiveDocumentAsync()
            .Returns((ReferenceItem)null);

        var sut = new ReferenceManager(
            [],
            watcher,
            provider,
            Substitute.For<IWorkspaceFileWatcher>(),
            Substitute.For<IFileReferenceBuilder>(),
            Substitute.For<ISymbolReferenceWatcher>(),
            new TestExecutionPipeline(),
            errorHandler);

        // Act
        var result = await sut.GetActiveDocumentAsync();

        // Assert
        result.Should().BeNull();

        await provider.Received(1).GetActiveDocumentAsync();// Exact 1 time GetActiveDocumentAsync is called
    }
}