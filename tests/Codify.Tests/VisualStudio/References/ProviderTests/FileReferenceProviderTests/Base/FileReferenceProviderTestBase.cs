using Codify.Core.Interfaces;
using Codify.TestCommon.Fakes.VisualStudio;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.References.Providers;
using EnvDTE;
using EnvDTE80;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Codify.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests.Base;
#pragma warning disable VSTHRD010
public abstract class FileReferenceProviderTestBase
{
    protected IVisualStudioServices VisualStudioServices = null!;
    protected IWorkspaceContext WorkspaceContext = null!;
    protected IFileSystem FileSystem = null!;
    protected IUiThreadDispatcher UiThreadDispatcher = null!;

    protected DTE2 Dte = null!;

    [SetUp]
    public virtual void SetUp()
    {
        VisualStudioServices = Substitute.For<IVisualStudioServices>();
        WorkspaceContext = Substitute.For<IWorkspaceContext>();
        FileSystem = Substitute.For<IFileSystem>();
        UiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();

        Dte = Substitute.For<DTE2>();

        WorkspaceContext.SolutionName.Returns("Codify");

        VisualStudioServices
            .GetDteAsync()
            .Returns(Task.FromResult(Dte));
    }
    /// <summary>
    /// Creates and returns a new instance of the FileReferenceProvider configured with the required Visual Studio
    /// services and context.
    /// Sut = System Under Test.
    /// </summary>
    /// <remarks>This method sets up the FileReferenceProvider with dependencies such as VisualStudioServices,
    /// WorkspaceContext, FileSystem, and UiThreadDispatcher, ensuring it is ready for use in file reference
    /// operations.</remarks>
    /// <returns>A FileReferenceProvider instance initialized for managing file references within the Visual Studio environment.</returns>
    protected virtual FileReferenceProvider CreateSut()
    {
        return new FileReferenceProvider(
            VisualStudioServices,
            WorkspaceContext,
            FileSystem,
            UiThreadDispatcher);
    }

    protected void SetActiveDocument(
        string filePath,
        string content)
    {
        var document = Substitute.For<Document>();

        document.FullName.Returns(filePath);

        Dte.ActiveDocument.Returns(document);

        FileSystem.Exists(filePath).Returns(true);
        FileSystem.ReadAllText(filePath).Returns(content);
    }

    protected void SetSolution(params Project[] projects)
    {
        var solution = Substitute.For<Solution>();

        solution.IsOpen.Returns(true);

        var fakeProjects = FakeProjects.Create(projects);

        solution.Projects.Returns(fakeProjects);

        Dte.Solution.Returns(solution);
    }
}