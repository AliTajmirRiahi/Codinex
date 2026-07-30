using System.Threading.Tasks;
using Codify.Core.Models.WorkspaceChanges;
using Codify.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests;

[TestFixture]
public class WorkspaceStateValidationRuleTests_Success
    : WorkspaceStateValidationRuleBaseTests
{
    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenCreateFilePathIsValidAsync()
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

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenEditFilePathIsValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new EditFileChange
                {
                    FilePath = "Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenDeleteFilePathIsValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteFileChange
                {
                    FilePath = "Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenRenameFilePathsAreValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameFileChange
                {
                    FilePath = "Program.cs",
                    NewFileName = "Program2.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenMoveFilePathsAreValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveFileChange
                {
                    SourcePath = "Program.cs",
                    DestinationPath = @"Folder\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenCreateDirectoryPathIsValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateDirectoryChange
                {
                    DirectoryPath = "Folder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenDeleteDirectoryPathIsValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteDirectoryChange
                {
                    DirectoryPath = "Folder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenRenameDirectoryPathsAreValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameDirectoryChange
                {
                    OldPath = "Folder",
                    NewPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ValidateAsync_ShouldSucceed_WhenMoveDirectoryPathsAreValidAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveDirectoryChange
                {
                    SourcePath = "Folder",
                    DestinationPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}