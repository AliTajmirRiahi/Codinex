using Codinex.Core.Models.References;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.References.Providers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using Codinex.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests.Base;

namespace Codinex.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests
{
    [TestFixture]
    public class FileReferenceProvider_ActiveDocumentByPathTests : FileReferenceProviderTestBase
    {
        [Test]
        public async Task GetActiveDocumentAsync_Should_ReturnReferenceItem_ForExistingFileAsync()
        {
            // Arrange
            var provider = CreateSut();

            var filePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.cs");

            WorkspaceFileService.Exists(filePath).Returns(true);

            // Act
            var result = await provider.GetActiveDocumentAsync(filePath);

            // Assert
            result.Should().NotBeNull();

            result.Name.Should().Be("Active Document");
            result.Type.Should().Be(ReferenceKind.File);
            result.Value.Should().Be(filePath);

            result.Metadata.Should().NotBeNull();
            result.Metadata.FilePath.Should().Be(filePath);
            result.Metadata.ProjectName.Should().Be("Codinex");

            result.Icon.Should().Be("fileTypes/file_type_csharp");

            // References are built from the path and outline; file bodies are never read.
            WorkspaceFileService.DidNotReceive().Read(Arg.Any<string>());
        }

        [Test]
        public async Task GetActiveDocumentAsync_Should_ReturnDefaultIcon_ForUnknownExtensionAsync()
        {
            // Arrange
            var provider = CreateSut();

            var filePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.unknown");

            WorkspaceFileService.Exists(filePath).Returns(true);
            WorkspaceFileService.Read(filePath).Returns("test");

            // Act
            var result = await provider.GetActiveDocumentAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.Icon.Should().Be("fileTypes/default_file");
            result.Description.Should().Be(Path.GetFileName(filePath));
        }

        [Test]
        public async Task GetActiveDocumentAsync_Should_ReturnNull_WhenFileDoesNotExistAsync()
        {
            // Arrange
            var provider = CreateSut();

            var filePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.cs");

            // Act
            var result = await provider.GetActiveDocumentAsync(filePath);

            // Assert
            result.Should().BeNull();
        }

    }
}
