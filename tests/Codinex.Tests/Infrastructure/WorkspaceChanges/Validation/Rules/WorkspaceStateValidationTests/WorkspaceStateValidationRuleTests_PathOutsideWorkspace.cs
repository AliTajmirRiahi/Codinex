using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests;

[TestFixture]
public class WorkspaceStateValidationRuleTests_PathOutsideWorkspace
    : WorkspaceStateValidationRuleBaseTests
{
    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenCreateFileEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateFileChange
                {
                    FilePath = @"..\..\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenEditFileEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new EditFileChange
                {
                    FilePath = @"..\..\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenDeleteFileEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteFileChange
                {
                    FilePath = @"..\..\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenRenameFileEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameFileChange
                {
                    FilePath = @"..\..\Program.cs",
                    NewFileName = "Program2.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenMoveFileEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveFileChange
                {
                    SourcePath = @"..\..\Program.cs",
                    DestinationPath = @"Folder\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenCreateDirectoryEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateDirectoryChange
                {
                    DirectoryPath = @"..\..\Folder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenDeleteDirectoryEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteDirectoryChange
                {
                    DirectoryPath = @"..\..\Folder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenRenameDirectoryEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameDirectoryChange
                {
                    OldPath = @"..\..\Folder",
                    NewPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnPathOutsideWorkspace_WhenMoveDirectoryEscapesWorkspaceAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveDirectoryChange
                {
                    SourcePath = @"..\..\Folder",
                    DestinationPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.PathOutsideWorkspace);
    }
}