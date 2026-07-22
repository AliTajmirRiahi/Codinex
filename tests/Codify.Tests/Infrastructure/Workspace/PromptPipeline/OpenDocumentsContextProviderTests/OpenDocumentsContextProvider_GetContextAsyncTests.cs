using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Tests.Infrastructure.Workspace.PromptPipeline.OpenDocumentsContextProviderTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.OpenDocumentsContextProviderTests;

[TestFixture]
public class OpenDocumentsContextProvider_GetContextAsyncTests
    : OpenDocumentsContextProviderTestBase
{
    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenContextIsNullAsync()
    {
        // Arrange
        var sut = CreateSut();

        OpenDocumentsContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns((OpenDocumentsContext)null!);

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();

        OpenDocumentsFormatter
            .DidNotReceive()
            .Format(Arg.Any<OpenDocumentsContext>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenNoDocumentsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        OpenDocumentsContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns(new OpenDocumentsContext
            {
                Documents = new List<OpenDocumentItem>()
            });

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeTrue();

        OpenDocumentsFormatter
            .DidNotReceive()
            .Format(Arg.Any<OpenDocumentsContext>());
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

        OpenDocumentsContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns(new OpenDocumentsContext
            {
                Documents = new List<OpenDocumentItem>
                {
                    new()
                }
            });

        // Act
        var result = await sut.GetContextAsync(
            request,
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeTrue();

        OpenDocumentsFormatter
            .DidNotReceive()
            .Format(Arg.Any<OpenDocumentsContext>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnPromptContext_WhenDocumentsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        var context = new OpenDocumentsContext
        {
            Documents = new List<OpenDocumentItem>
            {
                new()
            }
        };

        OpenDocumentsContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
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

        var context = new OpenDocumentsContext
        {
            Documents = new List<OpenDocumentItem>
            {
                new()
            }
        };

        OpenDocumentsContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
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

        OpenDocumentsContextProvider
            .GetContextAsync(Arg.Any<CancellationToken>())
            .Returns(new OpenDocumentsContext
            {
                Documents = new List<OpenDocumentItem>()
            });

        // Act
        await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            source.Token);

        // Assert
        await OpenDocumentsContextProvider
            .Received(1)
            .GetContextAsync(
                Arg.Is(source.Token));
    }

    [Test]
    public void GetContextAsync_Should_Throw_WhenCancellationRequested()
    {
        // Arrange
        var sut = CreateSut();

        using var source = new CancellationTokenSource();
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