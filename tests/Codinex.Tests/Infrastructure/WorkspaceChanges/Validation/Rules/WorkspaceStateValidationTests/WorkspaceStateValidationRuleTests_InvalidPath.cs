using System.IO;
using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests.Base;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests;

[TestFixture]
public class WorkspaceStateValidationRuleTests_InvalidPath
    : WorkspaceStateValidationRuleBaseTests
{
    private static readonly string InvalidPath =
        $"Folder{Path.GetInvalidPathChars()[0]}File";

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenCreateFileContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateFileChange
                {
                    FilePath = InvalidPath
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenEditFileContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new EditFileChange
                {
                    FilePath = InvalidPath
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenDeleteFileContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteFileChange
                {
                    FilePath = InvalidPath
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenRenameFileContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameFileChange
                {
                    FilePath = InvalidPath,
                    NewFileName = "New.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenMoveFileContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveFileChange
                {
                    SourcePath = InvalidPath,
                    DestinationPath = "Folder\\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenCreateDirectoryContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateDirectoryChange
                {
                    DirectoryPath = InvalidPath
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenDeleteDirectoryContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteDirectoryChange
                {
                    DirectoryPath = InvalidPath
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenRenameDirectoryContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameDirectoryChange
                {
                    OldPath = InvalidPath,
                    NewPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnInvalidPath_WhenMoveDirectoryContainsInvalidCharactersAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveDirectoryChange
                {
                    SourcePath = InvalidPath,
                    DestinationPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.InvalidPath);
    }
}