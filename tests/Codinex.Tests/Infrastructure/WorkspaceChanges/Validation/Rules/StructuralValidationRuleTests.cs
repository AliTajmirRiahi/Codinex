using System;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Validation.Rules;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules;

[TestFixture]
public class StructuralValidationRuleTests
{
    private StructuralValidationRule _sut = null!;

    [SetUp]
    public virtual void SetUp()
    {
        _sut = CreateSut();
    }

    protected virtual StructuralValidationRule CreateSut()
    {
        return new StructuralValidationRule();
    }

    [Test]
    public async Task ValidateAsync_ShouldThrow_WhenWorkspaceChangeSetIsNullAsync()
    {
        Func<Task> act = () => _sut.ValidateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenChangeSetIsEmptyAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidChange);
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenChangeIdIsEmptyAsync()
    {
        var change = new CreateFileChange
        {
            Id = Guid.Empty,
            FilePath = "Test.cs"
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenCreateFileChangeIsValidAsync()
    {
        var change = new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "Test.cs"
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCreateFilePathIsEmptyAsync()
    {
        var change = new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = string.Empty
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDeleteFilePathIsEmptyAsync()
    {
        var change = new DeleteFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = ""
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenEditFileContainsNoTextChangesAsync()
    {
        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "Test.cs"
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenEditFileChangeIsValidAsync()
    {
        var change = new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "Test.cs"
        };

        change.TextChanges.Add(new TextFileChange
        {
            Id = Guid.NewGuid(),
            Order = 1,
            Search = "old",
            Content = "new"
        });

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenRenameFileHasNoNewNameAsync()
    {
        var change = new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "Old.cs",
            NewFileName = ""
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenMoveFileHasNoDestinationAsync()
    {
        var change = new MoveFileChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "A.cs",
            DestinationPath = ""
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCreateDirectoryPathIsEmptyAsync()
    {
        var change = new CreateDirectoryChange
        {
            Id = Guid.NewGuid(),
            DirectoryPath = ""
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDeleteDirectoryPathIsEmptyAsync()
    {
        var change = new DeleteDirectoryChange
        {
            Id = Guid.NewGuid(),
            DirectoryPath = ""
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenMoveDirectoryDestinationIsEmptyAsync()
    {
        var change = new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "Folder1",
            DestinationPath = ""
        };

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(change);

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
    }
}