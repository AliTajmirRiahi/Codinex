using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests;

[TestFixture]
public sealed class WorkspaceChangeApplierTests_Failure
    : WorkspaceChangeApplierBaseTests
{
    [Test]
    public async Task ApplyAsync_WhenAChangeFails_ShouldStopProcessingAsync()
    {
        var change1 = new RenameFileChange();
        var change2 = new DeleteFileChange();

        var expected =
            WorkspaceChangeResult.Failed(
                new WorkspaceChangeError());

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
            .Returns(expected);

        var sut = CreateSut();

        var result =
            await sut.ApplyAsync(workspaceChangeSet);

        result.Should().BeSameAs(expected);

        await WorkspaceChangeHandlerInvoker
            .Received(1)
            .InvokeAsync(change1, Arg.Any<CancellationToken>());

        await WorkspaceChangeHandlerInvoker
            .DidNotReceive()
            .InvokeAsync(change2, Arg.Any<CancellationToken>());
    }
}