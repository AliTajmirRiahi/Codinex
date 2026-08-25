using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Validation.Rules;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules;

[TestFixture]
public class FileSystemValidationRuleTests
{
    private IWorkspaceFileService _workspaceFileService = null!;
    private FileSystemValidationRule _sut = null!;

    [SetUp]
    public virtual void SetUp()
    {
        _workspaceFileService = Substitute.For<IWorkspaceFileService>();

        _sut = CreateSut();
    }

    protected virtual FileSystemValidationRule CreateSut()
    {
        return new FileSystemValidationRule(_workspaceFileService);
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCreateFileAlreadyExistsAsync()
    {
        _workspaceFileService.FileExists("Test.cs").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Test.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenCreateFileDoesNotExistAsync()
    {
        _workspaceFileService.FileExists("Test.cs").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Test.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenEditFileDoesNotExistAsync()
    {
        _workspaceFileService.FileExists("Test.cs").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new EditFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Test.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenEditFileExistsAsync()
    {
        _workspaceFileService.FileExists("Test.cs").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new EditFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Test.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDeleteFileDoesNotExistAsync()
    {
        _workspaceFileService.FileExists("Test.cs").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Test.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenDeleteFileExistsAsync()
    {
        _workspaceFileService.FileExists("Test.cs").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Test.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenRenameFileSourceDoesNotExistAsync()
    {
        _workspaceFileService.FileExists("Old.cs").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Old.cs",
                    NewFileName = "New.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenRenameFileSourceExistsAsync()
    {
        _workspaceFileService.FileExists("Old.cs").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameFileChange
                {
                    Id = Guid.NewGuid(),
                    FilePath = "Old.cs",
                    NewFileName = "New.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenMoveFileSourceDoesNotExistAsync()
    {
        _workspaceFileService.FileExists("Old.cs").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveFileChange
                {
                    Id = Guid.NewGuid(),
                    SourcePath = "Old.cs",
                    DestinationPath = "New.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenMoveFileSourceExistsAsync()
    {
        _workspaceFileService.FileExists("Old.cs").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveFileChange
                {
                    Id = Guid.NewGuid(),
                    SourcePath = "Old.cs",
                    DestinationPath = "New.cs"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenCreateDirectoryAlreadyExistsAsync()
    {
        _workspaceFileService.DirectoryExists("Folder").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    DirectoryPath = "Folder"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenCreateDirectoryDoesNotExistAsync()
    {
        _workspaceFileService.DirectoryExists("Folder").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    DirectoryPath = "Folder"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenDeleteDirectoryDoesNotExistAsync()
    {
        _workspaceFileService.DirectoryExists("Folder").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    DirectoryPath = "Folder"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenDeleteDirectoryExistsAsync()
    {
        _workspaceFileService.DirectoryExists("Folder").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    DirectoryPath = "Folder"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenRenameDirectorySourceDoesNotExistAsync()
    {
        _workspaceFileService.DirectoryExists("Old").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    OldPath = "Old",
                    NewPath = "New"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenRenameDirectorySourceExistsAsync()
    {
        _workspaceFileService.DirectoryExists("Old").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    OldPath = "Old",
                    NewPath = "New"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldFail_WhenMoveDirectorySourceDoesNotExistAsync()
    {
        _workspaceFileService.DirectoryExists("Old").Returns(false);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    SourcePath = "Old",
                    DestinationPath = "New"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenMoveDirectorySourceExistsAsync()
    {
        _workspaceFileService.DirectoryExists("Old").Returns(true);

        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveDirectoryChange
                {
                    Id = Guid.NewGuid(),
                    SourcePath = "Old",
                    DestinationPath = "New"
                }
            }
        };

        var result = await _sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}