using Codinex.VisualStudio.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests.Base;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests;

[TestFixture]
public sealed class CreateFileChangeHandlerTests_EdgeCases
    : CreateFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldCreateEmptyFile_WhenContentIsNullAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var change = new CreateFileChange
        {
            FilePath = filePath,
            Content = null
        };

        WorkspaceFileService
            .Exists(filePath)
            .Returns(false);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                string.Empty,
                Arg.Any<System.Text.Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldCreateEmptyFile_WhenContentIsEmptyAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var change = new CreateFileChange
        {
            FilePath = filePath,
            Content = string.Empty
        };

        WorkspaceFileService
            .Exists(filePath)
            .Returns(false);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        await WorkspaceFileService.Received(1)
            .WriteAsync(
                filePath,
                string.Empty,
                Arg.Any<System.Text.Encoding>(),
                Arg.Any<CancellationToken>());
    }
}