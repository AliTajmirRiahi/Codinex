using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models.References;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.References.Providers.Base;
using Microsoft.CodeAnalysis;

namespace Codinex.TestCommon.Fakes.VisualStudio
{
    public sealed class TestRoslynReferenceProvider(
        IVisualStudioServices visualStudio,
        IUiThreadDispatcher uiThreadDispatcher,
        IWorkspaceIgnoreService? workspaceIgnoreService = null)
        : RoslynReferenceProviderBase(visualStudio, uiThreadDispatcher, workspaceIgnoreService ?? NeverIgnoreWorkspaceIgnoreService.Instance)
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

    internal sealed class NeverIgnoreWorkspaceIgnoreService : IWorkspaceIgnoreService
    {
        public static readonly NeverIgnoreWorkspaceIgnoreService Instance = new();

        public bool ShouldIgnore(string filePath) => false;
    }
}