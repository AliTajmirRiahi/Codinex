using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.References;
using Codinex.TestCommon.Builders.VisualStudio;
using Codinex.TestCommon.Fakes.VisualStudio;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.References;

namespace Codinex.Tests.VisualStudio.References
{
    [TestFixture]
    public class RoslynSymbolReferenceWatcherTests
    {
        private static ReferenceItem MakeItem(string id, string content) => new()
        {
            Id = id,
            Name = id,
            Type = ReferenceKind.Method,
            Metadata = new ReferenceMetadata { Content = content }
        };

        private static RoslynSymbolReferenceWatcher CreateSut(
            TestRoslynReferenceProvider provider,
            out IErrorHandler errorHandler)
        {
            var visualStudio = Substitute.For<IVisualStudioServices>();
            var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();
            uiThreadDispatcher.SwitchToMainThreadAsync().Returns(Task.CompletedTask);
            errorHandler = Substitute.For<IErrorHandler>();

            return new RoslynSymbolReferenceWatcher(
                visualStudio,
                uiThreadDispatcher,
                [provider],
                errorHandler);
        }

        [Test]
        public async Task DiffDocumentAsync_Should_RaiseReferenceAdded_ForNewItemAsync()
        {
            var scenario = new RoslynScenarioBuilder()
                .WithDocument(@"C:\Codinex\Program.cs", "class Program { void Foo() {} }")
                .Build();

            var visualStudio = Substitute.For<IVisualStudioServices>();
            var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();
            uiThreadDispatcher.SwitchToMainThreadAsync().Returns(Task.CompletedTask);

            var provider = new TestRoslynReferenceProvider(visualStudio, uiThreadDispatcher)
            {
                OnExtractAsync = (_, _) =>
                    Task.FromResult<IReadOnlyList<ReferenceItem>>([MakeItem("method:a", "void Foo() {}")])
            };

            var sut = CreateSut(provider, out _);

            ReferenceItem added = null;
            sut.ReferenceAdded += (_, e) => added = e.Item;

            await sut.DiffDocumentAsync(scenario.Document);

            added.Should().NotBeNull();
            added.Id.Should().Be("method:a");
        }

        [Test]
        public async Task DiffDocumentAsync_Should_RaiseReferenceUpdated_WhenContentChangesAsync()
        {
            var scenario = new RoslynScenarioBuilder()
                .WithDocument(@"C:\Codinex\Program.cs", "class Program { void Foo() {} }")
                .Build();

            var visualStudio = Substitute.For<IVisualStudioServices>();
            var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();
            uiThreadDispatcher.SwitchToMainThreadAsync().Returns(Task.CompletedTask);

            var provider = new TestRoslynReferenceProvider(visualStudio, uiThreadDispatcher)
            {
                OnExtractAsync = (_, _) =>
                    Task.FromResult<IReadOnlyList<ReferenceItem>>([MakeItem("method:a", "void Foo() {}")])
            };

            var sut = CreateSut(provider, out _);

            await sut.DiffDocumentAsync(scenario.Document);

            provider.OnExtractAsync = (_, _) =>
                Task.FromResult<IReadOnlyList<ReferenceItem>>([MakeItem("method:a", "void Foo() { Bar(); }")]);

            ReferenceItem updated = null;
            var addedCount = 0;
            sut.ReferenceAdded += (_, _) => addedCount++;
            sut.ReferenceUpdated += (_, e) => updated = e.Item;

            await sut.DiffDocumentAsync(scenario.Document);

            addedCount.Should().Be(0);
            updated.Should().NotBeNull();
            updated.Metadata.Content.Should().Be("void Foo() { Bar(); }");
        }

        [Test]
        public async Task DiffDocumentAsync_Should_NotRaiseAnyEvent_WhenNothingChangedAsync()
        {
            var scenario = new RoslynScenarioBuilder()
                .WithDocument(@"C:\Codinex\Program.cs", "class Program { void Foo() {} }")
                .Build();

            var visualStudio = Substitute.For<IVisualStudioServices>();
            var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();
            uiThreadDispatcher.SwitchToMainThreadAsync().Returns(Task.CompletedTask);

            var provider = new TestRoslynReferenceProvider(visualStudio, uiThreadDispatcher)
            {
                OnExtractAsync = (_, _) =>
                    Task.FromResult<IReadOnlyList<ReferenceItem>>([MakeItem("method:a", "void Foo() {}")])
            };

            var sut = CreateSut(provider, out _);

            await sut.DiffDocumentAsync(scenario.Document);

            var raised = false;
            sut.ReferenceAdded += (_, _) => raised = true;
            sut.ReferenceUpdated += (_, _) => raised = true;
            sut.ReferenceRemoved += (_, _) => raised = true;

            await sut.DiffDocumentAsync(scenario.Document);

            raised.Should().BeFalse();
        }

        [Test]
        public async Task DiffDocumentAsync_Should_RaiseReferenceRemoved_WhenItemDisappearsAsync()
        {
            var scenario = new RoslynScenarioBuilder()
                .WithDocument(@"C:\Codinex\Program.cs", "class Program { void Foo() {} }")
                .Build();

            var visualStudio = Substitute.For<IVisualStudioServices>();
            var uiThreadDispatcher = Substitute.For<IUiThreadDispatcher>();
            uiThreadDispatcher.SwitchToMainThreadAsync().Returns(Task.CompletedTask);

            var provider = new TestRoslynReferenceProvider(visualStudio, uiThreadDispatcher)
            {
                OnExtractAsync = (_, _) =>
                    Task.FromResult<IReadOnlyList<ReferenceItem>>([MakeItem("method:a", "void Foo() {}")])
            };

            var sut = CreateSut(provider, out _);

            await sut.DiffDocumentAsync(scenario.Document);

            provider.OnExtractAsync = (_, _) =>
                Task.FromResult<IReadOnlyList<ReferenceItem>>([]);

            string removedId = null;
            sut.ReferenceRemoved += (_, e) => removedId = e.Id;

            await sut.DiffDocumentAsync(scenario.Document);

            removedId.Should().Be("method:a");
        }
    }
}
