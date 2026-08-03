using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests;

[TestFixture]
public sealed class WorkspaceChangeApplierTests_Success
    : WorkspaceChangeApplierBaseTests
{
    [Test]
    public async Task ApplyAsync_WhenAllChangesSucceed_ShouldReturnSuccessfulAsync()
    {
        var change1 = new RenameFileChange();
        var change2 = new DeleteFileChange();

        var workspaceChangeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                change1,
                change2
            }
        };

        WorkspaceChangeHandlerInvoker
            .InvokeAsync(change1, Arg.Any<CancellationToken>())
            .Returns(WorkspaceChangeResult.Successful());

        WorkspaceChangeHandlerInvoker
            .InvokeAsync(change2, Arg.Any<CancellationToken>())
            .Returns(WorkspaceChangeResult.Successful());

        var sut = CreateSut();

        var result =
            await sut.ApplyAsync(workspaceChangeSet);

        result.Success.Should().BeTrue();

        await WorkspaceChangeHandlerInvoker
            .Received(1)
            .InvokeAsync(change1, Arg.Any<CancellationToken>());

        await WorkspaceChangeHandlerInvoker
            .Received(1)
            .InvokeAsync(change2, Arg.Any<CancellationToken>());
    }
}