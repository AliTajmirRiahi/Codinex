using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests.Base;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.RenameFileChangeHandlerTests;

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