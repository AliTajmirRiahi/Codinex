using Codify.Core.Abstractions;
using Codify.Core.Models;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Codify.Infrastructure.References.Providers
{
    public sealed class ClassReferenceProvider : IReferenceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public ClassReferenceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            // Ensure execution starts on the VS UI thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var workspace = GetWorkspace();
            if (workspace == null)
            {
                return Array.Empty<ReferenceItem>();
            }

            var dte = await GetDteAsync();
            if (dte?.Solution == null)
            {
                return Array.Empty<ReferenceItem>();
            }

            var currentSolution = workspace.CurrentSolution;
            var result = new List<ReferenceItem>();

            foreach (var project in currentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (!IsSupportedDocument(document))
                    {
                        continue;
                    }

                    // Await the static task without using implicit or explicit instance methods
                    var classItems = await ExtractClassesFromDocumentAsync(project, document).ConfigureAwait(false);
                    result.AddRange(classItems);
                }
            }

            return result;
        }

        private VisualStudioWorkspace GetWorkspace()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel ?? Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;

            return componentModel?.GetService<VisualStudioWorkspace>();
        }

        private async Task<DTE> GetDteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            if (dte == null)
            {
                dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            }

            return dte;
        }

        // Static implementation to avoid capturing state machine context
        private static bool IsSupportedDocument(Microsoft.CodeAnalysis.Document document)
        {
            if (document == null)
            {
                return false;
            }

            var filePath = document.FilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            return filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        // Fully static implementation ensures the generated async state machine class does not need to reference 'this'
        private static async Task<IReadOnlyList<ReferenceItem>> ExtractClassesFromDocumentAsync(
            Microsoft.CodeAnalysis.Project project,
            Microsoft.CodeAnalysis.Document document)
        {
            var results = new List<ReferenceItem>();

            // Capture the syntax root and semantic model safely in local task context
            var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            if (syntaxRoot == null)
            {
                return results;
            }

            var sourceText = await document.GetTextAsync().ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            var classDeclarations = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();
            results.AddRange(classDeclarations.Select(classDeclaration => BuildClassReferenceItem(project, document, sourceText, semanticModel, classDeclaration)).Where(item => item != null));

            return results;
        }

        private static ReferenceItem BuildClassReferenceItem(
            Microsoft.CodeAnalysis.Project project,
            Microsoft.CodeAnalysis.Document document,
            SourceText sourceText,
            SemanticModel semanticModel,
            ClassDeclarationSyntax classDeclaration)
        {
            var symbol = semanticModel?.GetDeclaredSymbol(classDeclaration);
            var className = classDeclaration.Identifier.ValueText;

            var namespaceName = symbol?.ContainingNamespace?.IsGlobalNamespace == false
                ? symbol.ContainingNamespace.ToDisplayString()
                : GetContainingNamespaceName(classDeclaration);

            var containingType = symbol?.ContainingType?.ToDisplayString()
                                 ?? GetContainingTypeName(classDeclaration);

            var containerName = !string.IsNullOrWhiteSpace(containingType)
                ? containingType
                : namespaceName;

            var signature = symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                           ?? BuildFallbackSignature(classDeclaration);

            var body = GetClassBody(classDeclaration);
            var fullCode = classDeclaration.NormalizeWhitespace().ToFullString();

            var lineSpan = sourceText.Lines.GetLinePositionSpan(classDeclaration.Span);
            var startLine = lineSpan.Start.Line + 1;
            var endLine = lineSpan.End.Line + 1;

            var filePath = document.FilePath ?? string.Empty;
            var fileName = Path.GetFileName(filePath);
            var projectName = project?.Name ?? string.Empty;

            return new ReferenceItem
            {
                Id = $"file:{Guid.NewGuid()}",
                Name = className,
                Description = fileName,
                Type = ReferenceKind.Class,
                Icon = "symbols/symbol-class", // Placeholder for actual icon representation
                Color = "--vs-viz-surface-gold-medium-color",
                Metadata = new ReferenceMetadata()
                {
                    FilePath = filePath,
                    ProjectName = projectName,
                    ContainerName = containerName,
                    Signature = signature,
                    Body = body,
                    Content = fullCode,
                    StartLine = startLine,
                    EndLine = endLine
                }
            };
        }

        private static string GetContainingNamespaceName(ClassDeclarationSyntax classDeclaration)
        {
            var current = classDeclaration.Parent;

            while (current != null)
            {
                if (current is BaseNamespaceDeclarationSyntax namespaceDeclaration)
                {
                    return namespaceDeclaration.Name.ToString();
                }

                current = current.Parent;
            }

            return string.Empty;
        }

        private static string GetContainingTypeName(ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.Parent is TypeDeclarationSyntax parentType)
            {
                return parentType.Identifier.ValueText;
            }

            return string.Empty;
        }

        private static string BuildFallbackSignature(ClassDeclarationSyntax classDeclaration)
        {
            var modifiers = classDeclaration.Modifiers.ToString();
            var className = classDeclaration.Identifier.ValueText;
            var typeParameters = classDeclaration.TypeParameterList?.ToString() ?? string.Empty;
            var baseList = classDeclaration.BaseList?.ToString() ?? string.Empty;
            var constraintClauses = classDeclaration.ConstraintClauses.Any()
                ? " " + string.Join(" ", classDeclaration.ConstraintClauses.Select(c => c.ToString()))
                : string.Empty;

            var prefix = string.IsNullOrWhiteSpace(modifiers)
                ? "class"
                : $"{modifiers} class";

            return $"{prefix} {className}{typeParameters} {baseList}{constraintClauses}".Trim();
        }

        private static string GetClassBody(ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.OpenBraceToken.IsMissing || classDeclaration.CloseBraceToken.IsMissing)
            {
                return string.Empty;
            }

            var members = classDeclaration.Members;
            if (members.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(
                Environment.NewLine + Environment.NewLine,
                members.Select(member => member.NormalizeWhitespace().ToFullString()));
        }
    }
}
