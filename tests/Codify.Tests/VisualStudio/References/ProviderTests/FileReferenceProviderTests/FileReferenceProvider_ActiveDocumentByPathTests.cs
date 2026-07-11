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

namespace Codify.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests
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

            FileSystem.Exists(filePath).Returns(true);
            FileSystem.ReadAllText(filePath).Returns("class Test {}");

            // Act
            var result = await provider.GetActiveDocumentAsync(filePath);

            // Assert
            result.Should().NotBeNull();

            result.Name.Should().Be("Active Document");
            result.Type.Should().Be(ReferenceKind.File);
            result.Value.Should().Be(filePath);

            result.Metadata.Should().NotBeNull();
            result.Metadata.FilePath.Should().Be(filePath);
            result.Metadata.ProjectName.Should().Be("Codify");
            result.Metadata.Content.Should().Be("class Test {}");

            result.Icon.Should().Be("fileTypes/file_type_csharp");
        }

        [Test]
        public async Task GetActiveDocumentAsync_Should_ReturnDefaultIcon_ForUnknownExtensionAsync()
        {
            // Arrange
            var provider = CreateSut();

            var filePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.unknown");

            FileSystem.Exists(filePath).Returns(true);
            FileSystem.ReadAllText(filePath).Returns("test");

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

        [Test]
        public async Task GetActiveDocumentAsync_Should_ReturnEmptyContent_WhenReadAllTextThrowsAsync()
        {
            // Arrange
            var provider = CreateSut();

            const string filePath = @"C:\Temp\Test.cs";

            FileSystem.Exists(filePath).Returns(true);

            FileSystem
                .When(x => x.ReadAllText(filePath))
                .Do(_ => throw new IOException("Unable to read file."));

            // Act
            var result = await provider.GetActiveDocumentAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.Metadata.Content.Should().BeEmpty();

            FileSystem.Received(1).Exists(filePath);
            FileSystem.Received(1).ReadAllText(filePath);
        }

        [Test]
        public async Task GetActiveDocumentAsync_Should_SetContentFromFileSystemAsync()
        {
            // Arrange
            var provider = CreateSut();

            const string filePath = @"C:\Temp\Test.cs";
            const string expectedContent = "public class Test { }";

            FileSystem.Exists(filePath).Returns(true);
            FileSystem.ReadAllText(filePath).Returns(expectedContent);

            // Act
            var result = await provider.GetActiveDocumentAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.Metadata.Content.Should().Be(expectedContent);

            FileSystem.Received(1).Exists(filePath);
            FileSystem.Received(1).ReadAllText(filePath);
        }
    }
}
