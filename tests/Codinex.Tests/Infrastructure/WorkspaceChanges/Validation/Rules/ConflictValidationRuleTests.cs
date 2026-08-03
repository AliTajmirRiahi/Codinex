using System;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Validation.Rules;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules;

[TestFixture]
public class ConflictValidationRuleTests
{
    private ConflictValidationRule _sut = null!;

    [SetUp]
    public virtual void SetUp()
    {
        _sut = CreateSut();
    }

    protected virtual ConflictValidationRule CreateSut()
    {
        return new ConflictValidationRule();
    }

    [Test]
    public async Task ValidateAsync_ShouldThrow_WhenWorkspaceChangeSetIsNullAsync()
    {
        Func<Task> act = () => _sut.ValidateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenNoConflictsExistAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs"
        });

        changeSet.Changes.Add(new DeleteFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "B.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateChangeIdExistsAsync()
    {
        var id = Guid.NewGuid();

        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new CreateFileChange
        {
            Id = id,
            FilePath = "A.cs"
        });

        changeSet.Changes.Add(new DeleteFileChange
        {
            Id = id,
            FilePath = "B.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateCreateFileExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs"
        });

        changeSet.Changes.Add(new CreateFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateDeleteFileExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new DeleteFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs"
        });

        changeSet.Changes.Add(new DeleteFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateCreateDirectoryExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new CreateDirectoryChange
        {
            Id = Guid.NewGuid(),
            DirectoryPath = "Models"
        });

        changeSet.Changes.Add(new CreateDirectoryChange
        {
            Id = Guid.NewGuid(),
            DirectoryPath = "Models"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateDeleteDirectoryExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new DeleteDirectoryChange
        {
            Id = Guid.NewGuid(),
            DirectoryPath = "Models"
        });

        changeSet.Changes.Add(new DeleteDirectoryChange
        {
            Id = Guid.NewGuid(),
            DirectoryPath = "Models"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateEditFileExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs"
        });

        changeSet.Changes.Add(new EditFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateRenameFileTargetExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs",
            NewFileName = "C.cs"
        });

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "B.cs",
            NewFileName = "C.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateMoveFileDestinationExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new MoveFileChange
        {
            Id = Guid.NewGuid(),
            SourcePath = @"A\A.cs",
            DestinationPath = @"C\A.cs"
        });

        changeSet.Changes.Add(new MoveFileChange
        {
            Id = Guid.NewGuid(),
            SourcePath = @"B\B.cs",
            DestinationPath = @"C\A.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateRenameDirectoryTargetExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new RenameDirectoryChange
        {
            Id = Guid.NewGuid(),
            OldPath = "FolderA",
            NewPath = "Models"
        });

        changeSet.Changes.Add(new RenameDirectoryChange
        {
            Id = Guid.NewGuid(),
            OldPath = "FolderB",
            NewPath = "Models"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDuplicateMoveDirectoryDestinationExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderA",
            DestinationPath = "Shared"
        });

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderB",
            DestinationPath = "Shared"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCircularFileRenameExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs",
            NewFileName = "B.cs"
        });

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "B.cs",
            NewFileName = "A.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCircularFileMoveExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new MoveFileChange
        {
            Id = Guid.NewGuid(),
            SourcePath = @"FolderA\File.cs",
            DestinationPath = @"FolderB\File.cs"
        });

        changeSet.Changes.Add(new MoveFileChange
        {
            Id = Guid.NewGuid(),
            SourcePath = @"FolderB\File.cs",
            DestinationPath = @"FolderA\File.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCircularDirectoryRenameExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new RenameDirectoryChange
        {
            Id = Guid.NewGuid(),
            OldPath = "FolderA",
            NewPath = "FolderB"
        });

        changeSet.Changes.Add(new RenameDirectoryChange
        {
            Id = Guid.NewGuid(),
            OldPath = "FolderB",
            NewPath = "FolderA"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCircularDirectoryMoveExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderA",
            DestinationPath = "FolderB"
        });

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderB",
            DestinationPath = "FolderA"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenRenameChainIsNotCircularAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs",
            NewFileName = "B.cs"
        });

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "B.cs",
            NewFileName = "C.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenMoveChainIsNotCircularAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderA",
            DestinationPath = "FolderB"
        });

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderB",
            DestinationPath = "FolderC"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenThreeNodeCircularFileRenameExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "A.cs",
            NewFileName = "B.cs"
        });

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "B.cs",
            NewFileName = "C.cs"
        });

        changeSet.Changes.Add(new RenameFileChange
        {
            Id = Guid.NewGuid(),
            FilePath = "C.cs",
            NewFileName = "A.cs"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenThreeNodeCircularDirectoryMoveExistsAsync()
    {
        var changeSet = new WorkspaceChangeSet();

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderA",
            DestinationPath = "FolderB"
        });

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderB",
            DestinationPath = "FolderC"
        });

        changeSet.Changes.Add(new MoveDirectoryChange
        {
            Id = Guid.NewGuid(),
            SourcePath = "FolderC",
            DestinationPath = "FolderA"
        });

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }
}