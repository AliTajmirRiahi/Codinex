using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests;

[TestFixture]
public sealed class WorkspaceChangeApplierTests_Empty
    : WorkspaceChangeApplierBaseTests
{
    [Test]
    public async Task ApplyAsync_WhenNoChangesExist_ShouldReturnSuccessfulAsync()
    {
        var workspaceChangeSet = new WorkspaceChangeSet();

        var sut = CreateSut();

        var result =
            await sut.ApplyAsync(workspaceChangeSet);

        result.Success.Should().BeTrue();

        await WorkspaceChangeHandlerInvoker
            .DidNotReceiveWithAnyArgs()
            .InvokeAsync(null!);
    }
}