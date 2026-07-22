using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;
using Microsoft.CodeAnalysis;
using DiagnosticSeverity = Codify.Core.Models.DiagnosticSeverity;
using MicrosoftDiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;

namespace Codify.VisualStudio.Workspace.Providers
{
    /// <summary>
    /// Provides diagnostics from the current workspace.
    /// </summary>
    public sealed class DiagnosticsProvider(
        IVisualStudioServices visualStudio,
        IActiveDocumentProvider activeDocumentProvider)
        : VsServiceBase(visualStudio),
            IDiagnosticsProvider
    {
        public async Task<IReadOnlyList<DiagnosticItem>> GetDiagnosticsAsync(
            DiagnosticsScope scope,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return scope switch
            {
                DiagnosticsScope.CurrentDocument => await GetCurrentDocumentDiagnosticsAsync(cancellationToken),
                DiagnosticsScope.CurrentProject => await GetCurrentProjectDiagnosticsAsync(cancellationToken),
                DiagnosticsScope.Solution => await GetSolutionDiagnosticsAsync(cancellationToken),
                _ => (IReadOnlyList<DiagnosticItem>)[]
            };
        }
        private async Task<IReadOnlyList<DiagnosticItem>> GetCurrentDocumentDiagnosticsAsync(CancellationToken cancellationToken)
        {
            var document = await GetActiveRoslynDocumentAsync();

            if (document == null)
            {
                return [];
            }

            var compilation =
                await document.Project.GetCompilationAsync(cancellationToken);

            if (compilation == null)
            {
                return [];
            }

            return compilation
                .GetDiagnostics(cancellationToken)
                .Where(IsSupportedDiagnostic)
                .Where(x =>
                    string.Equals(
                        x.Location.SourceTree?.FilePath,
                        document.FilePath,
                        StringComparison.OrdinalIgnoreCase))
                .Select(x => MapDiagnostic(x, document.Project.Name))
                .ToList();
        }

        private async Task<IReadOnlyList<DiagnosticItem>> GetCurrentProjectDiagnosticsAsync(CancellationToken cancellationToken)
        {
            var document = await GetActiveRoslynDocumentAsync();

            if (document == null)
            {
                return [];
            }

            return await GetProjectDiagnosticsAsync(
                document.Project,
                cancellationToken);
        }

        private const int DefaultMaximumDiagnostics = 100;

        private async Task<IReadOnlyList<DiagnosticItem>> GetSolutionDiagnosticsAsync(
            CancellationToken cancellationToken)
        {
            var workspace = await GetWorkspaceAsync();

            if (workspace == null)
            {
                return [];
            }

            var tasks = workspace.CurrentSolution.Projects
                .Select(project => GetProjectDiagnosticsAsync(
                    project,
                    cancellationToken));

            var diagnostics =
                await Task.WhenAll(tasks);

            return diagnostics
                .SelectMany(x => x)
                .GroupBy(x => new
                {
                    x.Id,
                    x.FilePath,
                    x.Line,
                    x.Column,
                    x.Severity
                })
                .Select(x => x.First())
                .OrderBy(x => GetSeverityOrder(x.Severity))
                .Take(DefaultMaximumDiagnostics)
                .ToList();
        }

        private async Task<IReadOnlyList<DiagnosticItem>> GetProjectDiagnosticsAsync(
            Project project,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var compilation =
                    await project.GetCompilationAsync(cancellationToken);

                if (compilation == null)
                {
                    return [];
                }

                return compilation
                    .GetDiagnostics(cancellationToken)
                    .Where(IsSupportedDiagnostic)
                    .Select(diagnostic =>
                        MapDiagnostic(
                            diagnostic,
                            project.Name))
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Ignore projects that cannot produce diagnostics.
                return [];
            }
        }

        private static DiagnosticItem MapDiagnostic(
            Diagnostic diagnostic,
            string projectName)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();

            return new DiagnosticItem
            {
                Id = diagnostic.Id,

                ProjectName = projectName,

                FilePath = lineSpan.Path,

                Line = lineSpan.StartLinePosition.Line + 1,

                Column = lineSpan.StartLinePosition.Character + 1,

                Severity = MapSeverity(diagnostic.Severity),

                Message = diagnostic.GetMessage()
            };
        }

        private static bool IsSupportedDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return false;
            }

            if (diagnostic.Location == Location.None)
            {
                return false;
            }

            if (!diagnostic.Location.IsInSource)
            {
                return false;
            }

            var path = diagnostic.Location.SourceTree?.FilePath;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var fileName = Path.GetFileName(path);

            if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return diagnostic.Severity is MicrosoftDiagnosticSeverity.Error or MicrosoftDiagnosticSeverity.Warning;
        }
        private static int GetSeverityOrder(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => 0,
                DiagnosticSeverity.Warning => 1,
                _ => 2
            };
        }
        private async Task<Document> GetActiveRoslynDocumentAsync()
        {
            var workspace = await GetWorkspaceAsync();

            if (workspace == null)
            {
                return null;
            }

            var activeDocument =
                await activeDocumentProvider.GetActiveDocumentAsync();

            if (activeDocument?.Metadata?.FilePath == null)
            {
                return null;
            }

            return workspace.CurrentSolution
                .Projects
                .SelectMany(x => x.Documents)
                .FirstOrDefault(x =>
                    string.Equals(
                        x.FilePath,
                        activeDocument.Metadata.FilePath,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static DiagnosticSeverity MapSeverity(MicrosoftDiagnosticSeverity severity)
        {
            return severity switch
            {
                MicrosoftDiagnosticSeverity.Error => DiagnosticSeverity.Error,
                MicrosoftDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                MicrosoftDiagnosticSeverity.Info => DiagnosticSeverity.Info,
                _ => DiagnosticSeverity.Hidden
            };
        }
    }

}