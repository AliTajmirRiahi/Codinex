using System.Collections.Generic;
using System.Threading.Tasks;

using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.References;

using FluentAssertions;

using NSubstitute;

using NUnit.Framework;

namespace Codify.Tests.VisualStudio.References;

[TestFixture]
public class ReferenceManagerGetReferencesTests
{
    [Test]
    public async Task GetAllReferencesAsync_Should_ReturnEmptyCollection_When_NoProvidersExistAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var activeDocumentProvider = Substitute.For<IActiveDocumentProvider>();
        var pipeline = Substitute.For<IExecutionPipeline>();
        var errorHandler = Substitute.For<IErrorHandler>();


        var sut = new ReferenceManager(
            new List<IReferenceProvider>(),
            watcher,
            activeDocumentProvider,
            pipeline,
            errorHandler);

        // Act
        var result = await sut.GetAllReferencesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllReferencesAsync_Should_ReturnReferences_FromSingleProviderAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var activeDocumentProvider = Substitute.For<IActiveDocumentProvider>();
        var pipeline = Substitute.For<IExecutionPipeline>();
        var errorHandler = Substitute.For<IErrorHandler>();

        var provider = Substitute.For<IReferenceProvider>();

        var expected = new List<ReferenceItem>
        {
            new ReferenceItem
            {
                Name = "Program.cs"
            }
        };

        provider
            .GetReferencesAsync()
            .Returns(Task.FromResult<IReadOnlyList<ReferenceItem>>(expected));

        var sut = new ReferenceManager(
            [provider],
            watcher,
            activeDocumentProvider,
            pipeline,
            errorHandler);

        // Act
        var result = await sut.GetAllReferencesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeSameAs(expected[0]);

        await provider.Received(1).GetReferencesAsync();
    }

    [Test]
    public async Task GetAllReferencesAsync_Should_MergeResults_FromMultipleProvidersAsync()
    {
        // Arrange
        var watcher = Substitute.For<IActiveDocumentWatcher>();
        var activeDocumentProvider = Substitute.For<IActiveDocumentProvider>();
        var pipeline = Substitute.For<IExecutionPipeline>();
        var errorHandler = Substitute.For<IErrorHandler>();

        var provider1 = Substitute.For<IReferenceProvider>();
        var provider2 = Substitute.For<IReferenceProvider>();

        provider1
            .GetReferencesAsync()
            .Returns(Task.FromResult<IReadOnlyList<ReferenceItem>>(
                new List<ReferenceItem>
                {
                    new() { Name = "Class1.cs" }
                }));

        provider2
            .GetReferencesAsync()
            .Returns(Task.FromResult<IReadOnlyList<ReferenceItem>>(
                new List<ReferenceItem>
                {
                    new() { Name = "Class2.cs" }
                }));

        var sut = new ReferenceManager(
            [provider1, provider2],
            watcher,
            activeDocumentProvider,
            pipeline,
            errorHandler);

        // Act
        var result = await sut.GetAllReferencesAsync();

        // Assert
        result.Should().HaveCount(2);

        result[0].Name.Should().Be("Class1.cs");
        result[1].Name.Should().Be("Class2.cs");

        await provider1.Received(1).GetReferencesAsync();
        await provider2.Received(1).GetReferencesAsync();
    }
}