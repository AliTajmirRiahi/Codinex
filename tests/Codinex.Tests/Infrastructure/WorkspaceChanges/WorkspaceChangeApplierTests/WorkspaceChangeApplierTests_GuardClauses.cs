using System;
using System.Threading.Tasks;
using Codify.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests;

[TestFixture]
public sealed class WorkspaceChangeApplierTests_GuardClauses
    : WorkspaceChangeApplierBaseTests
{
    [Test]
    public async Task ApplyAsync_WhenWorkspaceChangeSetIsNull_ShouldThrowArgumentNullExceptionAsync()
    {
        var sut = CreateSut();

        Func<Task> act = async () =>
            await sut.ApplyAsync(null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }
}