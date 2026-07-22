using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Tests.Infrastructure.Workspace.PromptPipeline.GitContextProviderTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.GitContextProviderTests;
#pragma warning disable VSTHRD110
[TestFixture]
public class GitContextProvider_GetContextAsyncTests
    : GitContextProviderTestBase
{
    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenGitContextIsNullAsync()
    {
        // Arrange
        var sut = CreateSut();

        GitContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns((GitContext)null!);

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();

        GitContextFormatter
            .DidNotReceive()
            .Format(Arg.Any<GitContext>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenGitContainsNoFilesAsync()
    {
        // Arrange
        var sut = CreateSut();

        GitContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns(new GitContext
            {
                BranchName = "main",
                Files = new List<GitFileItem>()
            });

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeTrue();

        GitContextFormatter
            .DidNotReceive()
            .Format(Arg.Any<GitContext>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnPromptContext_WhenGitContextExistsAsync()
    {
        // Arrange
        var sut = CreateSut();

        var gitContext = new GitContext
        {
            BranchName = "feature/context",
            Files = new List<GitFileItem>
            {
                new()
                {
                    Path = "Program.cs",
                    Status = GitFileStatus.Modified
                }
            }
        };

        GitContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns(gitContext);

        GitContextFormatter
            .Format(gitContext)
            .Returns("formatted git");

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeFalse();
        result.Items.Should().ContainSingle();

        var item = result.Items[0];

        item.Kind.Should().Be(PromptContextKind.Git);
        item.Title.Should().Be("Git");
        item.Content.Should().Be("formatted git");
    }

    [Test]
    public async Task GetContextAsync_Should_CallFormatter_WhenGitContextExistsAsync()
    {
        // Arrange
        var sut = CreateSut();

        var gitContext = new GitContext
        {
            Files = new List<GitFileItem>
            {
                new()
            }
        };

        GitContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns(gitContext);

        GitContextFormatter
            .Format(gitContext)
            .Returns("formatted");

        // Act
        await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        GitContextFormatter
            .Received(1)
            .Format(gitContext);
    }

    [Test]
    public async Task GetContextAsync_Should_PassCancellationTokenAsync()
    {
        // Arrange
        var sut = CreateSut();

        using var source = new CancellationTokenSource();

        GitContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns(new GitContext
            {
                Files = new List<GitFileItem>()
            });

        // Act
        await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            source.Token);

        // Assert
        await GitContextProvider
            .Received(1)
            .GetContextAsync(
                Arg.Is(source.Token));
    }

    [Test]
    public void GetContextAsync_Should_Throw_WhenCancellationRequested()
    {
        // Arrange
        var sut = CreateSut();

        var source = new CancellationTokenSource();
        source.Cancel();

        // Act
        var action = () => sut.GetContextAsync(
            new WorkspaceContextRequest(),
            source.Token);

        // Assert
        action.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
#pragma warning restore VSTHRD110