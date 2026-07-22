using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Tests.Infrastructure.Workspace.PromptPipeline.DiagnosticsContextProviderTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.DiagnosticsContextProviderTests;
#pragma warning disable VSTHRD110
[TestFixture]
public class DiagnosticsContextProvider_GetContextAsyncTests
    : DiagnosticsContextProviderTestBase
{
    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenNoDiagnosticsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        DiagnosticsProvider
            .GetDiagnosticsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DiagnosticItem>());

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();

        DiagnosticsFormatter
            .DidNotReceive()
            .Format(Arg.Any<IReadOnlyList<DiagnosticItem>>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnPromptContext_WhenDiagnosticsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        var diagnostics = new List<DiagnosticItem>
        {
            new()
            {
                Id = "CS1002",
                Message = "; expected"
            }
        };

        DiagnosticsProvider
            .GetDiagnosticsAsync(Arg.Any<CancellationToken>())
            .Returns(diagnostics);

        DiagnosticsFormatter
            .Format(diagnostics)
            .Returns("formatted diagnostics");

        // Act
        var result = await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeFalse();

        result.Items.Should().ContainSingle();

        var item = result.Items[0];

        item.Kind.Should().Be(PromptContextKind.Diagnostics);
        item.Title.Should().Be("Diagnostics");
        item.Content.Should().Be("formatted diagnostics");
    }

    [Test]
    public async Task GetContextAsync_Should_CallFormatter_WhenDiagnosticsExistAsync()
    {
        // Arrange
        var sut = CreateSut();

        var diagnostics = new List<DiagnosticItem>
        {
            new()
        };

        DiagnosticsProvider
            .GetDiagnosticsAsync(Arg.Any<CancellationToken>())
            .Returns(diagnostics);

        DiagnosticsFormatter
            .Format(diagnostics)
            .Returns("formatted");

        // Act
        await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            CancellationToken.None);

        // Assert
        DiagnosticsFormatter
            .Received(1)
            .Format(diagnostics);
    }

    [Test]
    public async Task GetContextAsync_Should_PassCancellationTokenAsync()
    {
        // Arrange
        var sut = CreateSut();

        using var source = new CancellationTokenSource();

        DiagnosticsProvider
            .GetDiagnosticsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<DiagnosticItem>());

        // Act
        await sut.GetContextAsync(
            new WorkspaceContextRequest(),
            source.Token);

        // Assert
        await DiagnosticsProvider
            .Received(1)
            .GetDiagnosticsAsync(
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
