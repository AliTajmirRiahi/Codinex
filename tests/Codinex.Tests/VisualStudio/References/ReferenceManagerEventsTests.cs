using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.TestCommon.Fakes;
using Codify.VisualStudio.References;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codify.Tests.VisualStudio.References
{
    [TestFixture]
    public class ReferenceManagerEventsTests
    {
        [Test]
        public async Task ActiveDocumentChanged_Should_RequestDocumentUsingFilePathAsync()
        {
            // Arrange
            var watcher = Substitute.For<IActiveDocumentWatcher>();
            var provider = Substitute.For<IActiveDocumentProvider>();
            var errorHandler = Substitute.For<IErrorHandler>();

            var expected = new ReferenceItem
            {
                Name = "Program.cs"
            };

            provider
                .GetActiveDocumentAsync(@"C:\Project\Program.cs")
                .Returns(expected);

            var pipeline = new TestExecutionPipeline();

            _ = new ReferenceManager(
                [],
                watcher,
                provider,
                pipeline,
                errorHandler);

            // Act
            watcher.ActiveDocumentChanged += Raise.EventWith(
                watcher,
                new ActiveDocumentChangedEventArgs
                {
                    FilePath = @"C:\Project\Program.cs",
                    FileName = "Program.cs"
                });

            // Assert
            await provider
                .Received(1)
                .GetActiveDocumentAsync(@"C:\Project\Program.cs");
        }

        [Test]
        public async Task ActiveDocumentChanged_Should_UpdateCachedDocumentAsync()
        {
            // Arrange
            var watcher = Substitute.For<IActiveDocumentWatcher>();
            var provider = Substitute.For<IActiveDocumentProvider>();
            var errorHandler = Substitute.For<IErrorHandler>();

            var programDocument = new ReferenceItem
            {
                Name = "Program.cs"
            };

            var mainWindowDocument = new ReferenceItem
            {
                Name = "MainWindow.xaml"
            };

            provider
                .GetActiveDocumentAsync()
                .Returns(programDocument);

            provider
                .GetActiveDocumentAsync(@"C:\Project\MainWindow.xaml")
                .Returns(mainWindowDocument);

            var pipeline = new TestExecutionPipeline();

            var sut = new ReferenceManager(
                [],
                watcher,
                provider,
                pipeline,
                errorHandler);

            // Act
            // Step 1
            var first = await sut.GetActiveDocumentAsync();

            // Step 2
            watcher.ActiveDocumentChanged += Raise.EventWith(
                watcher,
                new ActiveDocumentChangedEventArgs
                {
                    FilePath = @"C:\Project\MainWindow.xaml",
                    FileName = "MainWindow.xaml"
                });

            // Step 3
            var second = await sut.GetActiveDocumentAsync();

            // Assert
            first.Should().BeSameAs(programDocument);

            second.Should().BeSameAs(mainWindowDocument);

            second.Should().NotBeSameAs(first);

            await provider
                .Received(1)
                .GetActiveDocumentAsync();

            await provider
                .Received(1)
                .GetActiveDocumentAsync(@"C:\Project\MainWindow.xaml");
        }

        [Test]
        public void ActiveDocumentChanged_Should_RaiseActiveDocumentUpdatedEvent()
        {
            // Arrange
            var watcher = Substitute.For<IActiveDocumentWatcher>();
            var provider = Substitute.For<IActiveDocumentProvider>();
            var errorHandler = Substitute.For<IErrorHandler>();

            var activeDocument = new ReferenceItem
            {
                Name = "MainWindow.xaml"
            };

            provider
                .GetActiveDocumentAsync(@"C:\Project\MainWindow.xaml")
                .Returns(activeDocument);

            var sut = new ReferenceManager(
                [],
                watcher,
                provider,
                new TestExecutionPipeline(),
                errorHandler);

            var raiseCount = 0;

            sut.ActiveDocumentUpdated += (_, _) =>
            {
                raiseCount++;
            };

            // Act
            watcher.ActiveDocumentChanged += Raise.EventWith(
                watcher,
                new ActiveDocumentChangedEventArgs
                {
                    FilePath = @"C:\Project\MainWindow.xaml",
                    FileName = "MainWindow.xaml"
                });

            // Assert
            raiseCount.Should().Be(1);
        }

        [Test]
        public void ActiveDocumentChanged_Should_PassUpdatedDocumentInEventArgs()
        {
            // Arrange
            var watcher = Substitute.For<IActiveDocumentWatcher>();
            var provider = Substitute.For<IActiveDocumentProvider>();
            var errorHandler = Substitute.For<IErrorHandler>();

            var activeDocument = new ReferenceItem
            {
                Name = "MainWindow.xaml"
            };

            provider
                .GetActiveDocumentAsync(@"C:\Project\MainWindow.xaml")
                .Returns(activeDocument);

            var sut = new ReferenceManager(
                [],
                watcher,
                provider,
                new TestExecutionPipeline(),
                errorHandler);

            ActiveDocumentUpdatedEventArgs eventArgs = null;

            sut.ActiveDocumentUpdated += (_, args) =>
            {
                eventArgs = args;
            };

            // Act
            watcher.ActiveDocumentChanged += Raise.EventWith(
                watcher,
                new ActiveDocumentChangedEventArgs
                {
                    FilePath = @"C:\Project\MainWindow.xaml",
                    FileName = "MainWindow.xaml"
                });

            // Assert
            eventArgs.Should().NotBeNull();
            eventArgs.ActiveDocument.Should().BeSameAs(activeDocument);
        }

        [Test]
        public async Task GetAllReferencesAsync_Should_IgnoreNullProviderResultsAsync()
        {
            // Arrange
            var watcher = Substitute.For<IActiveDocumentWatcher>();
            var activeDocumentProvider = Substitute.For<IActiveDocumentProvider>();
            var errorHandler = Substitute.For<IErrorHandler>();

            var provider1 = Substitute.For<IReferenceProvider>();
            var provider2 = Substitute.For<IReferenceProvider>();

            provider1
                .GetReferencesAsync()
                .Returns((IReadOnlyList<ReferenceItem>)null);

            var expected = new List<ReferenceItem>
            {
                new()
                {
                    Name = "Program.cs"
                }
            };

            provider2
                .GetReferencesAsync()
                .Returns(expected);

            var sut = new ReferenceManager(
                [provider1, provider2],
                watcher,
                activeDocumentProvider,
                new TestExecutionPipeline(),
                errorHandler);

            // Act
            var result = await sut.GetAllReferencesAsync();

            // Assert
            result.Should().HaveCount(1);
            result[0].Should().BeSameAs(expected[0]);
        }
        [Test]
        public async Task GetAllReferencesAsync_Should_HandleException_WhenProviderThrowsAsync()
        {
            // Arrange
            var watcher = Substitute.For<IActiveDocumentWatcher>();
            var activeDocumentProvider = Substitute.For<IActiveDocumentProvider>();
            var errorHandler = Substitute.For<IErrorHandler>();

            var provider = Substitute.For<IReferenceProvider>();

            var exception = new InvalidOperationException("Test exception");

            provider
                .When(x => x.GetReferencesAsync())
                .Do(_ => throw exception);

            var sut = new ReferenceManager(
                [provider],
                watcher,
                activeDocumentProvider,
                new TestExecutionPipeline(),
                errorHandler);

            // Act
            Func<Task> action = async () => await sut.GetAllReferencesAsync();

            // Assert
            await action
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Test exception");

            errorHandler
                .Received(1)
                .Handle(
                    exception,
                    nameof(ReferenceManager.GetAllReferencesAsync));
        }
    }
}
