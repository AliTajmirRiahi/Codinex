using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests.Base;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.References.Providers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Codify.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests;

#pragma warning disable VSTHRD010
[TestFixture]
public class FileReferenceProvider_ActiveDocumentTests : FileReferenceProviderTestBase
{
    [Test]
    public async Task GetActiveDocumentAsync_Should_SwitchToMainThreadAsync()
    {
        // Arrange
        Dte.ActiveDocument.Returns((EnvDTE.Document)null);

        var provider = CreateSut();

        // Act
        await provider.GetActiveDocumentAsync();

        // Assert
        await UiThreadDispatcher
            .Received(1)
            .SwitchToMainThreadAsync();
    }

    [Test]
    public async Task GetActiveDocumentAsync_Should_ReturnNull_WhenActiveDocumentIsNullAsync()
    {
        // Arrange
        Dte?.ActiveDocument.Returns((EnvDTE.Document)null);

        var provider = CreateSut();

        // Act
        var result = await provider.GetActiveDocumentAsync();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetActiveDocumentAsync_Should_ReturnNull_WhenActiveDocumentPathIsEmptyAsync()
    {
        // Arrange
        var document = Substitute.For<EnvDTE.Document>();
        document.FullName.Returns(string.Empty);

        Dte.ActiveDocument.Returns(document);

        var provider = CreateSut();

        // Act
        var result = await provider.GetActiveDocumentAsync();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetActiveDocumentAsync_Should_ReturnNull_WhenActiveDocumentFileDoesNotExistAsync()
    {
        // Arrange
        const string filePath = @"C:\Fake\Test.cs";

        var document = Substitute.For<EnvDTE.Document>();
        document.FullName.Returns(filePath);

        WorkspaceFileService.Exists(filePath).Returns(false);

        var provider = CreateSut();

        // Act
        var result = await provider.GetActiveDocumentAsync();

        // Assert
        result.Should().BeNull();
    }
}
#pragma warning restore VSTHRD010
