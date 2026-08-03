using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests.Base;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests;

[TestFixture]
public sealed class RenameFileChangeHandlerTests_GuardClauses
    : RenameFileChangeHandlerBaseTests
{
    [Test]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenChangeIsNullAsync()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        Func<Task<WorkspaceChangeResult>> action = () => sut.HandleAsync(
            null!,
            CancellationToken.None);

        // Assert
        await action.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithParameterName("change");
    }
}