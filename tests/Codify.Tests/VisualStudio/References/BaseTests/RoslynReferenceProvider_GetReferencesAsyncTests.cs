using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.TestCommon.Builders.VisualStudio;
using Codify.TestCommon.Fakes.VisualStudio;
using Codify.VisualStudio.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.LanguageServices;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Codify.Tests.VisualStudio.References.BaseTests;
#pragma warning disable VSTHRD010
[TestFixture]
public class RoslynReferenceProviderBaseTests
{
    [Test]
    public async Task GetReferencesAsync_Should_NotInvokeExtraction_WhenWorkspaceIsUnavailableAsync()
    {
        // Arrange
        var visualStudio = Substitute.For<IVisualStudioServices>();

        var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();

        visualStudio
            .GetWorkspaceAsync()
            .Returns((Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace)null);

        uiThreadDispatcher
            .SwitchToMainThreadAsync()
            .Returns(Task.CompletedTask);

        var sut = new TestRoslynReferenceProvider(visualStudio, uiThreadDispatcher);

        // Act
        var result = await sut.GetReferencesAsync();

        // Assert
        result.Should().BeEmpty();
        sut.ExtractCallCount.Should().Be(0);
    }

    [Test]
    public async Task GetReferencesAsync_Should_NotInvokeExtraction_WhenSolutionIsUnavailableAsync()
    {
        // Arrange
        var visualStudio = Substitute.For<IVisualStudioServices>();

        var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();

        var dte = Substitute.For<EnvDTE80.DTE2>();

        dte.Solution.Returns((EnvDTE.Solution)null);

        visualStudio
            .GetDteAsync()
            .Returns(Task.FromResult(dte));

        visualStudio
            .GetWorkspaceAsync()
            .Returns((Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace)null);

        uiThreadDispatcher
            .SwitchToMainThreadAsync()
            .Returns(Task.CompletedTask);

        var sut = new TestRoslynReferenceProvider(visualStudio, uiThreadDispatcher);

        // Act
        var result = await sut.GetReferencesAsync();

        // Assert
        result.Should().BeEmpty();
        sut.ExtractCallCount.Should().Be(0);
    }

    [Test]
    public async Task GetReferencesAsync_Should_IgnoreUnsupportedDocumentsAsync()
    {
        // Arrange
        var scenario = new RoslynScenarioBuilder()
            .WithProject("Codify")
            .WithDocument(@"C:\Project\Program.cs", "class Program { }")
            .WithDocument(@"C:\Project\Readme.txt", "Documentation")
            .Build();

        var visualStudio = Substitute.For<IVisualStudioServices>();
        var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();

        uiThreadDispatcher
            .SwitchToMainThreadAsync()
            .Returns(Task.CompletedTask);

        visualStudio
            .GetWorkspaceAsync()
            .Returns(scenario.Workspace);

        var dte = Substitute.For<EnvDTE80.DTE2>();
        dte.Solution.Returns(Substitute.For<EnvDTE.Solution>());

        visualStudio
            .GetDteAsync()
            .Returns(Task.FromResult(dte));

        var sut = new TestRoslynReferenceProvider(
            visualStudio,
            uiThreadDispatcher);

        sut.OnExtractAsync = (_, document) =>
        {
            IReadOnlyList<ReferenceItem> items =
            [
                new ReferenceItem
                {
                    Name = document.Name
                }
            ];

            return Task.FromResult(items);
        };

        // Act
        var result = await sut.GetReferencesAsync();

        // Assert
        result.Should().HaveCount(1);

        result.Single().Name.Should().Be("Program.cs");

        sut.ExtractCallCount.Should().Be(1);
    }
}