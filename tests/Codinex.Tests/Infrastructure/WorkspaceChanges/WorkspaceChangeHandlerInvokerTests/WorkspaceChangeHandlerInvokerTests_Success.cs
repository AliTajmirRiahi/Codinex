using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeHandlerInvokerTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeHandlerInvokerTests;

[TestFixture]
public sealed class WorkspaceChangeHandlerInvokerTests_Success
    : WorkspaceChangeHandlerInvokerBaseTests
{
    [Test]
    public async Task InvokeAsync_ShouldResolveHandler_AndInvokeHandleAsync()
    {
        // Arrange
        var workspaceChange = new RenameFileChange
        {
            FilePath = @"C:\Test\File.cs",
            NewFileName = "NewFile.cs"
        };

        var expected = WorkspaceChangeResult.Successful();

        var handler =
            Substitute.For<IWorkspaceChangeHandler<RenameFileChange>>();

        handler
            .HandleAsync(
                workspaceChange,
                Arg.Any<CancellationToken>())
            .Returns(expected);

        WorkspaceChangeHandlerResolver
            .Resolve<RenameFileChange>()
            .Returns(handler);

        var sut = CreateSut();

        // Act
        var result = await sut.InvokeAsync(workspaceChange);

        // Assert
        result.Should().BeSameAs(expected);

        WorkspaceChangeHandlerResolver
            .Received(1)
            .Resolve<RenameFileChange>();

        await handler
            .Received(1)
            .HandleAsync(
                workspaceChange,
                Arg.Any<CancellationToken>());
    }
}