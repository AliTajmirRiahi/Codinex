using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Codify.TestCommon.Builders.VisualStudio
{
    public sealed class RoslynScenario
    {
        public AdhocWorkspace Workspace { get; set; }

        public Solution Solution { get; set; }

        public Project Project { get; set; }

        /// <summary>
        /// Gets the last added document.
        /// </summary>
        public Document Document { get; set; }

        /// <summary>
        /// Gets all Roslyn documents.
        /// </summary>
        public IReadOnlyList<Document> Documents { get; set; }
    }

    public sealed class RoslynScenarioBuilder
    {
        private sealed class DocumentDefinition
        {
            public string FilePath { get; set; }

            public string Source { get; set; }
        }

        private readonly List<DocumentDefinition> _documents = [];

        private string _projectName = "Codify";

        public RoslynScenarioBuilder WithProject(string projectName)
        {
            _projectName = projectName;
            return this;
        }

        public RoslynScenarioBuilder WithDocument(
            string filePath,
            string source)
        {
            _documents.Add(new DocumentDefinition
            {
                FilePath = filePath,
                Source = source
            });

            return this;
        }

        public RoslynScenarioBuilder WithSource(string source)
        {
            return WithDocument(
                @"C:\Codify\Test.cs",
                source);
        }

        public RoslynScenario Build()
        {
            var workspace = new AdhocWorkspace();

            var projectId = ProjectId.CreateNewId();

            var solution = workspace.CurrentSolution
                .AddProject(
                    projectId,
                    _projectName,
                    _projectName,
                    LanguageNames.CSharp)
                .AddMetadataReference(
                    projectId,
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            foreach (var definition in _documents)
            {
                solution = solution.AddDocument(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(definition.FilePath),
                    SourceText.From(definition.Source),
                    filePath: definition.FilePath);
            }

            workspace.TryApplyChanges(solution);

            var project = workspace.CurrentSolution.GetProject(projectId);

            var documents = project.Documents.ToList();

            return new RoslynScenario
            {
                Workspace = workspace,
                Solution = workspace.CurrentSolution,
                Project = project,
                Document = documents.LastOrDefault(),
                Documents = documents
            };
        }
    }
}