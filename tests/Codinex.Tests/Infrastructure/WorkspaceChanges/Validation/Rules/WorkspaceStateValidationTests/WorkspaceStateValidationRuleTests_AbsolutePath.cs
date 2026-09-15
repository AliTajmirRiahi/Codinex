using System.Threading.Tasks;
using Codinex.Core.Models.WorkspaceChanges;
using Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests.Base;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests;

[TestFixture]
public class WorkspaceStateValidationRuleTests_AbsolutePath
    : WorkspaceStateValidationRuleBaseTests
{
    // Every path here is rooted, so the rule reaches its "absolute path outside the solution"
    // check, which reads SolutionPath as well as SolutionDirectory. The base fixture only stubs
    // SolutionDirectory; an unstubbed SolutionPath yields string.Empty, and path.Contains("") is
    // always true, which silently disables the check. The sibling fixtures pass relative paths
    // and short-circuit before this point, so the stub belongs here.
    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        WorkspaceContext.SolutionPath
            .Returns(@"C:\Workspace\TestSolution\TestSolution.sln");
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenCreateFileUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateFileChange
                {
                    FilePath = @"C:\Temp\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenEditFileUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new EditFileChange
                {
                    FilePath = @"C:\Temp\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenDeleteFileUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteFileChange
                {
                    FilePath = @"C:\Temp\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenRenameFileUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameFileChange
                {
                    FilePath = @"C:\Temp\Program.cs",
                    NewFileName = "Program2.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenMoveFileUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveFileChange
                {
                    SourcePath = @"C:\Temp\Program.cs",
                    DestinationPath = @"Folder\Program.cs"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenCreateDirectoryUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new CreateDirectoryChange
                {
                    DirectoryPath = @"C:\Temp\Folder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenDeleteDirectoryUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new DeleteDirectoryChange
                {
                    DirectoryPath = @"C:\Temp\Folder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenRenameDirectoryUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new RenameDirectoryChange
                {
                    OldPath = @"C:\Temp\Folder",
                    NewPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }

    [Test]
    public async Task ValidateAsync_ShouldReturnAbsolutePathNotAllowed_WhenMoveDirectoryUsesAbsolutePathAsync()
    {
        var changeSet = new WorkspaceChangeSet
        {
            Changes =
            {
                new MoveDirectoryChange
                {
                    SourcePath = @"C:\Temp\Folder",
                    DestinationPath = "NewFolder"
                }
            }
        };

        var result = await Sut.ValidateAsync(changeSet);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(WorkspaceChangeErrorCode.AbsolutePathNotAllowed);
    }
}