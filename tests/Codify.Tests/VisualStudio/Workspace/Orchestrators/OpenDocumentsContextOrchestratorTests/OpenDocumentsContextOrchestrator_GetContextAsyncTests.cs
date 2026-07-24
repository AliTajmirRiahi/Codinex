using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Tests.VisualStudio.Workspace.Orchestrators.OpenDocumentsContextOrchestratorTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.VisualStudio.Workspace.Orchestrators.OpenDocumentsContextOrchestratorTests;
#pragma warning disable VSTHRD110

[TestFixture]
public class OpenDocumentsContextOrchestrator_GetContextAsyncTests
    : OpenDocumentsContextOrchestratorTestBase
{
    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenContextIsNullAsync()
    {
        // Arrange
        var sut = CreateSut();

        OpenDocumentsProvider
            .GetOpenDocumentsAsync(Arg.Any<CancellationToken>())
            .Returns((List<ReferenceItem>)null!);

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();

        OpenDocumentsFormatter
            .DidNotReceive()
            .Format(Arg.Any<List<ReferenceItem>>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenNoDocumentsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        OpenDocumentsProvider
            .GetOpenDocumentsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReferenceItem>());

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeTrue();

        OpenDocumentsFormatter
            .DidNotReceive()
            .Format(Arg.Any<List<ReferenceItem>>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenOpenDocumentsReferenceAlreadyExistsAsync()
    {
        // Arrange
        var sut = CreateSut();

        var request = new WorkspaceContextRequest
        {
            References = new List<ReferenceItem>
            {
                new()
                {
                    Type = ReferenceKind.OpenDocuments
                }
            }
        };

        OpenDocumentsProvider
            .GetOpenDocumentsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReferenceItem>());

        // Act
        var result = await sut.GetContextAsync(
            request,
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeTrue();

        OpenDocumentsFormatter
            .DidNotReceive()
            .Format(Arg.Any<List<ReferenceItem>>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnPromptContext_WhenDocumentsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        IReadOnlyList<ReferenceItem> context =
        [
            new()
            {
                Name = "Program.cs"
            }
        ];

        OpenDocumentsProvider
            .GetOpenDocumentsAsync(Arg.Any<CancellationToken>())
            .Returns(context);

        OpenDocumentsFormatter
            .Format(context)
            .Returns("formatted documents");

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeFalse();
        result.Items.Should().ContainSingle();

        var item = result.Items[0];

        item.Kind.Should().Be(PromptContextKind.OpenDocuments);
        item.Title.Should().Be("Open Documents");
        item.Content.Should().Be("formatted documents");
    }

    [Test]
    public async Task GetContextAsync_Should_CallFormatter_WhenDocumentsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        IReadOnlyList<ReferenceItem> context =
        [
            new()
            {
                Name = "Program.cs"
            }
        ];

        OpenDocumentsProvider
            .GetOpenDocumentsAsync(Arg.Any<CancellationToken>())
            .Returns(context);

        OpenDocumentsFormatter
            .Format(context)
            .Returns("formatted");

        // Act
        await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        OpenDocumentsFormatter
            .Received(1)
            .Format(context);
    }

    [Test]
    public async Task GetContextAsync_Should_PassCancellationTokenAsync()
    {
        // Arrange
        var sut = CreateSut();

        using var source = new CancellationTokenSource();

        OpenDocumentsProvider
            .GetOpenDocumentsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReferenceItem>());

        // Act
        await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            source.Token);

        // Assert
        await OpenDocumentsProvider
            .Received(1)
            .GetOpenDocumentsAsync(
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
