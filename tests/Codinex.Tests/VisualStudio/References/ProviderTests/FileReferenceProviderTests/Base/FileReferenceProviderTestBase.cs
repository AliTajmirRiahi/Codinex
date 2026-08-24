using EnvDTE;
using EnvDTE80;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Codinex.Core.Interfaces;
using Codinex.TestCommon.Fakes.VisualStudio;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.References.Providers;

namespace Codinex.Tests.VisualStudio.References.ProviderTests.FileReferenceProviderTests.Base;
#pragma warning disable VSTHRD010
public abstract class FileReferenceProviderTestBase
{
    protected IVisualStudioServices VisualStudioServices = null!;
    protected IWorkspaceContext WorkspaceContext = null!;
    protected IWorkspaceFileService WorkspaceFileService = null!;
    protected ISourceFileElementService SourceFileElementService = null!;
    protected IUiThreadDispatcher UiThreadDispatcher = null!;
    protected IWorkspaceIgnoreService WorkspaceIgnoreService = null!;

    protected DTE2 Dte = null!;

    [SetUp]
    public virtual void SetUp()
    {
        VisualStudioServices = Substitute.For<IVisualStudioServices>();
        WorkspaceContext = Substitute.For<IWorkspaceContext>();
        //FileSystem = Substitute.For<IFileSystem>();
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
        SourceFileElementService = Substitute.For<ISourceFileElementService>();
        UiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();
        WorkspaceIgnoreService = Substitute.For<IWorkspaceIgnoreService>();
        WorkspaceIgnoreService.ShouldIgnore(Arg.Any<string>()).Returns(false);

        Dte = Substitute.For<DTE2>();

        WorkspaceContext.SolutionName.Returns("Codinex");

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
            WorkspaceFileService,
            SourceFileElementService,
            UiThreadDispatcher,
            WorkspaceIgnoreService);
    }

    protected void SetActiveDocument(
        string filePath,
        string content)
    {
        var document = Substitute.For<Document>();

        document.FullName.Returns(filePath);

        Dte.ActiveDocument.Returns(document);

        WorkspaceFileService.Exists(filePath).Returns(true);
        WorkspaceFileService.Read(filePath).Returns(content);
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