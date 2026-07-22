using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Workspace.Prompt;
using Codify.Tests.Infrastructure.Workspace.PromptPipeline.WorkspaceContextBuilderTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.WorkspaceContextBuilderTests;

[TestFixture]
public class WorkspaceContextBuilder_BuildAsyncTests
    : WorkspaceContextBuilderTestBase
{
    [Test]
    public async Task BuildAsync_Should_CallAllProviders_WhenProvidersExistAsync()
    {
        // Arrange
        var sut = CreateSut(Provider1, Provider2, Provider3);

        Provider1.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContextProviderResult());

        Provider2.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContextProviderResult());

        Provider3.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContextProviderResult());

        Composer.Compose(Arg.Any<List<ContextProviderResult>>())
            .Returns(new PromptContext());

        // Act
        await sut.BuildAsync(CreateRequest(), CancellationToken.None);

        // Assert
        await Provider1.Received(1)
            .GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>());

        await Provider2.Received(1)
            .GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>());

        await Provider3.Received(1)
            .GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BuildAsync_Should_PassRequestToProvidersAsync()
    {
        // Arrange
        var request = CreateRequest();

        var sut = CreateSut(Provider1);

        Provider1.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContextProviderResult());

        Composer.Compose(Arg.Any<List<ContextProviderResult>>())
            .Returns(new PromptContext());

        // Act
        await sut.BuildAsync(request, CancellationToken.None);

        // Assert
        await Provider1.Received(1)
            .GetContextAsync(
                Arg.Is(request),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BuildAsync_Should_PassCancellationTokenToProvidersAsync()
    {
        // Arrange
        var token = new CancellationTokenSource().Token;

        var sut = CreateSut(Provider1);

        Provider1.GetContextAsync(
                Arg.Any<WorkspaceContextRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ContextProviderResult());

        Composer.Compose(Arg.Any<List<ContextProviderResult>>())
            .Returns(new PromptContext());

        // Act
        await sut.BuildAsync(CreateRequest(), token);

        // Assert
        await Provider1.Received(1)
            .GetContextAsync(
                Arg.Any<WorkspaceContextRequest>(),
                Arg.Is(token));
    }

    [Test]
    public async Task BuildAsync_Should_PassProviderResultsToComposerAsync()
    {
        // Arrange
        var result1 = new ContextProviderResult();
        var result2 = new ContextProviderResult();

        var sut = CreateSut(Provider1, Provider2);

        Provider1.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(result1);

        Provider2.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(result2);

        Composer.Compose(Arg.Any<List<ContextProviderResult>>())
            .Returns(new PromptContext());

        // Act
        await sut.BuildAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Composer.Received(1)
            .Compose(Arg.Is<List<ContextProviderResult>>(x =>
                x.Count == 2 &&
                x.First() == result1 &&
                x.Last() == result2));
    }

    [Test]
    public async Task BuildAsync_Should_IgnoreNullProviderResultsAsync()
    {
        // Arrange
        var result = new ContextProviderResult();

        var sut = CreateSut(Provider1, Provider2);

        Provider1.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns((ContextProviderResult)null!);

        Provider2.GetContextAsync(Arg.Any<WorkspaceContextRequest>(), Arg.Any<CancellationToken>())
            .Returns(result);

        Composer.Compose(Arg.Any<List<ContextProviderResult>>())
            .Returns(new PromptContext());

        // Act
        await sut.BuildAsync(CreateRequest(), CancellationToken.None);

        // Assert
        Composer.Received(1)
            .Compose(Arg.Is<List<ContextProviderResult>>(x =>
                x.Count == 1 &&
                x.Single() == result));
    }

    [Test]
    public async Task BuildAsync_Should_ReturnComposerResultAsync()
    {
        // Arrange
        var expected = new PromptContext();

        var sut = CreateSut();

        Composer.Compose(Arg.Any<List<ContextProviderResult>>())
            .Returns(expected);

        // Act
        var result = await sut.BuildAsync(
            CreateRequest(),
            CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }
}