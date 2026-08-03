using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;
using Codify.Core.Workspace.Prompt;
using Codify.Tests.VisualStudio.Workspace.Orchestrators.CurrentDocumentContextOrchestratorTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.VisualStudio.Workspace.Orchestrators.CurrentDocumentContextOrchestratorTests;

[TestFixture]
public class CurrentDocumentContextOrchestrator_GetContextAsyncTests
    : CurrentDocumentContextOrchestratorTestBase
{
    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenActiveDocumentIsNullAsync()
    {
        // Arrange
        var provider = CreateSut();

        ActiveDocumentProvider
            .GetActiveDocumentAsync()
            .Returns((ReferenceItem?)null);

        // Act
        var result = await provider.GetContextAsync(
            CreateRequest(),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();

        ReferenceContextFormatter
            .DidNotReceive()
            .Format(Arg.Any<ReferenceItem>());
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnPromptContextItem_WhenActiveDocumentExistsAsync()
    {
        // Arrange
        var provider = CreateSut();

        var reference = new ReferenceItem
        {
            Name = "Program.cs",
            Type = ReferenceKind.File,
            Value = @"C:\Program.cs"
        };

        ActiveDocumentProvider
            .GetActiveDocumentAsync()
            .Returns(reference);

        ReferenceContextFormatter
            .Format(reference)
            .Returns("formatted content");

        // Act
        var result = await provider.GetContextAsync(
            CreateRequest(),
            CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);

        var item = result.Items[0];

        item.Kind.Should().Be(PromptContextKind.CurrentDocument);
        item.Title.Should().Be("Program.cs");
        item.Content.Should().Be("formatted content");

        ReferenceContextFormatter
            .Received(1)
            .Format(reference);
    }

    [Test]
    public async Task GetContextAsync_Should_ReturnEmpty_WhenDocumentAlreadyExistsInReferencesAsync()
    {
        // Arrange
        var provider = CreateSut();

        var reference = new ReferenceItem
        {
            Name = "Program.cs",
            Type = ReferenceKind.File,
            Value = @"C:\Program.cs"
        };

        ActiveDocumentProvider
            .GetActiveDocumentAsync()
            .Returns(reference);

        var request = CreateRequest();

        request.References = new List<ReferenceItem>
        {
            new()
            {
                Name = "Program.cs",
                Type = ReferenceKind.File,
                Value = @"C:\Program.cs"
            }
        };

        // Act
        var result = await provider.GetContextAsync(
            request,
            CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeTrue();

        ReferenceContextFormatter
            .DidNotReceive()
            .Format(Arg.Any<ReferenceItem>());
    }
}