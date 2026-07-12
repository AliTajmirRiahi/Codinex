using Codify.Core.Models;
using Codify.TestCommon.Fakes.VisualStudio;
using Codify.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests.Base;
using EnvDTE;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Codify.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests;
#pragma warning disable VSTHRD010
[TestFixture]
public class FileReferenceProvider_ReferenceCollectionTests : FileReferenceProviderTestBase
{
    [Test]
    public async Task GetReferencesAsync_Should_ReturnEmptyCollection_WhenDteIsNullAsync()
    {
        // Arrange

        VisualStudioServices
            .GetDteAsync()
            .Returns((EnvDTE80.DTE2)null);

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().NotBeNull();
        result.Should().BeEmpty();

        await UiThreadDispatcher
            .Received(1)
            .SwitchToMainThreadAsync();
    }

    [Test]
    public async Task GetReferencesAsync_Should_ReturnEmptyCollection_WhenSolutionIsNotOpenAsync()
    {
        // Arrange

        var solution = Substitute.For<EnvDTE.Solution>();
        solution.IsOpen.Returns(false);

        Dte.Solution.Returns(solution);

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().NotBeNull();
        result.Should().BeEmpty();

        await UiThreadDispatcher
            .Received(1)
            .SwitchToMainThreadAsync();

        await VisualStudioServices
            .Received(1)
            .GetDteAsync();
    }

    [Test]
    public async Task GetReferencesAsync_Should_NotAddNullActiveDocumentAsync()
    {
        // Arrange

        var solution = Substitute.For<EnvDTE.Solution>();
        solution.IsOpen.Returns(true);

        var projects = Substitute.For<EnvDTE.Projects>();

        Dte.Solution.Returns(solution);
        solution.Projects.Returns(projects);
        Dte.ActiveDocument.Returns((EnvDTE.Document)null);

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().NotBeNull();
        result.Should().BeEmpty();

        await UiThreadDispatcher
            .ReceivedWithAnyArgs()
            .SwitchToMainThreadAsync();
    }
    [Test]
    public async Task GetReferencesAsync_Should_ReturnOnlyActiveDocument_WhenSolutionHasNoProjectsAsync()
    {
        // Arrange
        const string filePath = @"C:\Fake\Test.cs";

        var document = Substitute.For<EnvDTE.Document>();
        document.FullName.Returns(filePath);

        var solution = Substitute.For<EnvDTE.Solution>();
        solution.IsOpen.Returns(true);

        var projects = Substitute.For<EnvDTE.Projects>();

        Dte.ActiveDocument.Returns(document);
        Dte.Solution.Returns(solution);
        solution.Projects.Returns(projects);

        FileSystem.Exists(filePath).Returns(true);
        FileSystem.ReadAllText(filePath).Returns("class Test {}");

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().HaveCount(1);

        var reference = result.Single();

        reference.Name.Should().Be("Active Document");
        reference.Type.Should().Be(ReferenceKind.File);
        reference.Value.Should().Be(filePath);

        reference.Metadata.ProjectName.Should().Be("Codify");
        reference.Metadata.Content.Should().Be("class Test {}");
    }

    [Test]
    public async Task GetReferencesAsync_Should_ReturnProjectFileReferencesAsync()
    {
        // Arrange

        WorkspaceContext.SolutionName.Returns("Codify");

        const string activeDocumentPath = @"C:\Solution\Program.cs";
        const string projectFilePath = @"C:\Solution\Services\UserService.cs";

        SetActiveDocument(
            activeDocumentPath,
            "Program");

        SetSolution(

            FakeProject.Create(
                "Codify.Core",
                FakeProjectItems.Create(

                    FakeProjectItem.Create(
                        projectFilePath,
                        "Codify.Core"))));

        FileSystem.ReadAllText(projectFilePath)
            .Returns("UserService");

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().HaveCount(2);

        result.Should().Contain(x =>
            x.Name == "Active Document" &&
            x.Value == activeDocumentPath);

        result.Should().Contain(x =>
            x.Name == "UserService.cs" &&
            x.Value == projectFilePath &&
            x.Metadata.ProjectName == "Codify.Core" &&
            x.Metadata.Content == "UserService");
    }

    [Test]
    public async Task GetReferencesAsync_Should_IgnoreNonPhysicalProjectItemsAsync()
    {
        // Arrange

        const string activeDocumentPath = @"C:\Solution\Program.cs";

        SetActiveDocument(
            activeDocumentPath,
            "Program");

        var projectItem = FakeProjectItem.CreateNonPhysical();

        SetSolution(
            FakeProject.Create(
                "Codify.Core",
                FakeProjectItems.Create(projectItem)));

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().HaveCount(1);

        var reference = result.Single();

        reference.Name.Should().Be("Active Document");
        reference.Type.Should().Be(ReferenceKind.File);
        reference.Value.Should().Be(activeDocumentPath);

        FileSystem.Received(1)
            .ReadAllText(activeDocumentPath);
    }

    [Test]
    public async Task GetReferencesAsync_Should_ProcessNestedProjectItemsAsync()
    {
        // Arrange

        const string activeDocumentPath = @"C:\Solution\Program.cs";
        const string nestedFilePath = @"C:\Solution\Services\UserService.cs";

        SetActiveDocument(
            activeDocumentPath,
            "Program");

        FileSystem.ReadAllText(nestedFilePath)
            .Returns("UserService");

        var childItem = FakeProjectItem.Create(
            nestedFilePath,
            "Codify.Core");

        var childProjectItems = FakeProjectItems.Create(childItem);

        var parentItem = FakeProjectItem.CreateFolder(
            childProjectItems);

        SetSolution(
            FakeProject.Create(
                "Codify.Core",
                FakeProjectItems.Create(parentItem)));

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().HaveCount(2);

        result.Should().Contain(x =>
            x.Name == "Active Document" &&
            x.Value == activeDocumentPath);

        result.Should().Contain(x =>
            x.Name == "UserService.cs" &&
            x.Value == nestedFilePath &&
            x.Metadata.ProjectName == "Codify.Core" &&
            x.Metadata.Content == "UserService");
    }

    [Test]
    public async Task GetReferencesAsync_Should_ProcessSubProjectItemsAsync()
    {
        // Arrange

        const string activeDocumentPath = @"C:\Solution\Program.cs";
        const string subProjectFilePath = @"C:\Solution\Shared\Logger.cs";

        SetActiveDocument(
            activeDocumentPath,
            "Program");

        FileSystem.ReadAllText(subProjectFilePath)
            .Returns("Logger");

        var subProject = FakeProject.Create(
            "Codify.Shared",
            FakeProjectItems.Create(
                FakeProjectItem.Create(
                    subProjectFilePath,
                    "Codify.Shared")));

        var projectItem = FakeProjectItem.CreateSubProject(subProject);

        SetSolution(
            FakeProject.Create(
                "Codify.Core",
                FakeProjectItems.Create(projectItem)));

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().HaveCount(2);

        result.Should().Contain(x =>
            x.Name == "Logger.cs" &&
            x.Value == subProjectFilePath &&
            x.Metadata.ProjectName == "Codify.Shared");
    }

    [Test]
    public async Task GetReferencesAsync_Should_ReturnEmptyContent_WhenReadingFileThrowsAsync()
    {
        // Arrange

        const string activeDocumentPath = @"C:\Solution\Program.cs";
        const string projectFilePath = @"C:\Solution\Services\UserService.cs";

        SetActiveDocument(
            activeDocumentPath,
            "Program");

        FileSystem
            .When(x => x.ReadAllText(projectFilePath))
            .Do(_ => throw new InvalidOperationException());

        SetSolution(
            FakeProject.Create(
                "Codify.Core",
                FakeProjectItems.Create(
                    FakeProjectItem.Create(
                        projectFilePath,
                        "Codify.Core"))));

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().HaveCount(2);

        var reference = result.Single(x => x.Name == "UserService.cs");

        reference.Metadata.Should().NotBeNull();
        reference.Metadata.Content.Should().BeEmpty();

        FileSystem.Received(1)
            .ReadAllText(projectFilePath);
    }

    [Test]
    public async Task GetReferencesAsync_Should_HandleProjectWithNullProjectItemsAsync()
    {
        // Arrange

        const string activeDocumentPath = @"C:\Solution\Program.cs";

        SetActiveDocument(
            activeDocumentPath,
            "Program");

        var project = FakeProject.Create(
            "Codify.Core",
            null);

        SetSolution(project);

        var sut = CreateSut();

        // Act

        var result = await sut.GetReferencesAsync();

        // Assert

        result.Should().HaveCount(1);

        result.Single().Name.Should().Be("Active Document");
    }
}
#pragma warning restore VSTHRD010
