using Codinex.Infrastructure.WorkspaceChanges;
using Codinex.VisualStudio.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests.Base;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.CreateFileChangeHandlerTests;

[TestFixture]
public sealed class CreateFileChangeHandlerTests_Failure
    : CreateFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldReturnFailure_WhenFileAlreadyExistsAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var change = new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            Content = "Hello"
        };

        var error = new WorkspaceChangeError();

        WorkspaceFileService
            .Exists(filePath)
            .Returns(true);

        WorkspaceChangeErrorFactory
            .Create(
                WorkspaceChangeErrorCode.FileAlreadyExists,
                filePath,
                change.Id,
                Arg.Any<string>())
            .Returns(error);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
    }

    [Test]
    public async Task HandleAsync_ShouldNotWriteFile_WhenFileAlreadyExistsAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var change = new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            Content = "Hello"
        };

        WorkspaceFileService
            .Exists(filePath)
            .Returns(true);

        WorkspaceChangeErrorFactory
            .Create(
                Arg.Any<WorkspaceChangeErrorCode>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string>())
            .Returns(new WorkspaceChangeError());

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        await WorkspaceFileService.DidNotReceive()
            .WriteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<System.Text.Encoding>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_ShouldCreateWorkspaceChangeError_WhenFileAlreadyExistsAsync()
    {
        // Arrange
        const string filePath = @"C:\Test\File.cs";

        var change = new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            Content = "Hello"
        };

        WorkspaceFileService
            .Exists(filePath)
            .Returns(true);

        WorkspaceChangeErrorFactory
            .Create(
                Arg.Any<WorkspaceChangeErrorCode>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string>())
            .Returns(new WorkspaceChangeError());

        var sut = CreateSut();

        // Act
        await sut.HandleAsync(
            change,
            CancellationToken.None);

        // Assert
        WorkspaceChangeErrorFactory.Received(1)
            .Create(
                WorkspaceChangeErrorCode.FileAlreadyExists,
                filePath,
                change.Id,
                Arg.Any<string>());
    }
}