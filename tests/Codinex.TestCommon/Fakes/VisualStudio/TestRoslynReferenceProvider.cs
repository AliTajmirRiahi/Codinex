using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.References.Providers.Base;
using Microsoft.CodeAnalysis;

namespace Codify.TestCommon.Fakes.VisualStudio
{
    public sealed class TestRoslynReferenceProvider(IVisualStudioServices visualStudio, IUiThreadDispatcher uiThreadDispatcher)
        : RoslynReferenceProviderBase(visualStudio, uiThreadDispatcher)
    {
        /// <summary>
        /// Gets the number of times ExtractReferencesAsync has been invoked.
        /// </summary>
        public int ExtractCallCount { get; private set; }

        /// <summary>
        /// Gets the last Roslyn project passed to ExtractReferencesAsync.
        /// </summary>
        public Project? LastProject { get; private set; }

        /// <summary>
        /// Gets the last Roslyn document passed to ExtractReferencesAsync.
        /// </summary>
        public Document? LastDocument { get; private set; }

        /// <summary>
        /// Allows tests to customize the extraction behavior.
        /// </summary>
        public Func<Project, Document, Task<IReadOnlyList<ReferenceItem>>>? OnExtractAsync { get; set; }

        protected override Task<IReadOnlyList<ReferenceItem>> ExtractReferencesAsync(
            Project project,
            Document document)
        {
            ExtractCallCount++;

            LastProject = project;
            LastDocument = document;

            return OnExtractAsync?.Invoke(project, document)
                   ?? Task.FromResult<IReadOnlyList<ReferenceItem>>([]);
        }
    }
}