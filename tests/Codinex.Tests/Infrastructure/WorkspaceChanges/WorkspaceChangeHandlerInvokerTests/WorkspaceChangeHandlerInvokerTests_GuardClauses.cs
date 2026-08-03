using System;
using System.Threading.Tasks;
using Codinex.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeHandlerInvokerTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeHandlerInvokerTests;

[TestFixture]
public sealed class WorkspaceChangeHandlerInvokerTests_GuardClauses
    : WorkspaceChangeHandlerInvokerBaseTests
{
    [Test]
    public async Task InvokeAsync_WhenWorkspaceChangeIsNull_ShouldThrowArgumentNullExceptionAsync()
    {
        var sut = CreateSut();

        Func<Task> act = async () =>
            await sut.InvokeAsync(null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }
}