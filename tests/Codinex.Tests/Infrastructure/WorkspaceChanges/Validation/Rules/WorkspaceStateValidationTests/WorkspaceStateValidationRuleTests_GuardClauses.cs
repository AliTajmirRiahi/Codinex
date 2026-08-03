using System;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Infrastructure.WorkspaceChanges.Validation.Rules;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests;

[TestFixture]
public class WorkspaceStateValidationRuleTests_GuardClauses
    : WorkspaceStateValidationRuleBaseTests
{

    [Test]
    public async Task ValidateAsync_ShouldThrowArgumentNullException_WhenWorkspaceChangeSetIsNullAsync()
    {
        Func<Task> act = () => Sut.ValidateAsync(null!);

        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task ValidateAsync_ShouldThrowOperationCanceledException_WhenCancellationRequestedAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateFileChange
                {
                    FilePath = "Program.cs"
                }
            }
        };

        var cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        Func<Task> act = () => Sut.ValidateAsync(
            changeSet,
            cancellationTokenSource.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}