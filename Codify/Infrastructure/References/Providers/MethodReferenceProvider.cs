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
    public sealed class MethodReferenceProvider : IReferenceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public MethodReferenceProvider(IServiceProvider serviceProvider)
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
                    var methodItems = await ExtractMethodsFromDocumentAsync(project, document).ConfigureAwait(false);
                    result.AddRange(methodItems);
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

            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE ?? Package.GetGlobalService(typeof(DTE)) as DTE;
            return dte;
        }

        // Static implementation to avoid capturing state machine context
        private static bool IsSupportedDocument(Microsoft.CodeAnalysis.Document document)
        {
            if (document == null)
                return false;

            var filePath = document.FilePath;
            return !string.IsNullOrWhiteSpace(filePath) && filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        // Fully static implementation ensures the generated async state machine class does not need to reference 'this'
        private static async Task<IReadOnlyList<ReferenceItem>> ExtractMethodsFromDocumentAsync(
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

            var methodDeclarations = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();
            results.AddRange(methodDeclarations.Select(methodDeclaration => BuildMethodReferenceItem(project, document, sourceText, semanticModel, methodDeclaration)).Where(item => item != null));

            return results;
        }

        private static ReferenceItem BuildMethodReferenceItem(
            Microsoft.CodeAnalysis.Project project,
            Microsoft.CodeAnalysis.Document document,
            SourceText sourceText,
            SemanticModel semanticModel,
            MethodDeclarationSyntax methodDeclaration)
        {
            var symbol = semanticModel?.GetDeclaredSymbol(methodDeclaration);
            var methodName = methodDeclaration.Identifier.ValueText;

            var containingType = symbol?.ContainingType?.ToDisplayString()
                                 ?? GetContainingTypeName(methodDeclaration);

            var signature = symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                           ?? BuildFallbackSignature(methodDeclaration);

            var body = GetMethodBody(methodDeclaration);
            var fullCode = methodDeclaration.NormalizeWhitespace().ToFullString();

            var lineSpan = sourceText.Lines.GetLinePositionSpan(methodDeclaration.Span);
            var startLine = lineSpan.Start.Line + 1;
            var endLine = lineSpan.End.Line + 1;

            var filePath = document.FilePath ?? string.Empty;
            var fileName = Path.GetFileName(filePath);
            var projectName = project?.Name ?? string.Empty;

            return new ReferenceItem
            {
                Id = BuildStableId(filePath, containingType, methodName, startLine),
                Name = methodName,
                Description = fileName,
                Type = ReferenceKind.Method,
                Icon = "symbols/symbol-method", // Placeholder for actual icon representation
                Metadata = new ReferenceMetadata()
                {
                    FilePath = filePath,
                    ProjectName = projectName,
                    ContainerName = containingType,
                    Signature = signature,
                    Body = body,
                    Content = fullCode,
                    StartLine = startLine,
                    EndLine = endLine
                }
            };
        }

        private static string GetContainingTypeName(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.Parent is TypeDeclarationSyntax typeDeclaration)
            {
                return typeDeclaration.Identifier.ValueText;
            }

            return "UnknownType";
        }

        private static string BuildFallbackSignature(MethodDeclarationSyntax methodDeclaration)
        {
            var returnType = methodDeclaration.ReturnType?.ToString() ?? "void";
            var methodName = methodDeclaration.Identifier.ValueText;
            var parameters = string.Join(", ", methodDeclaration.ParameterList.Parameters.Select(p => p.ToString()));

            return $"{returnType} {methodName}({parameters})";
        }

        private static string GetMethodBody(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.Body != null)
            {
                return methodDeclaration.Body.NormalizeWhitespace().ToFullString();
            }

            if (methodDeclaration.ExpressionBody != null)
            {
                return "=> " + methodDeclaration.ExpressionBody.Expression.NormalizeWhitespace().ToFullString() + ";";
            }

            return string.Empty;
        }

        private static string BuildStableId(string filePath, string containingType, string methodName, int startLine)
        {
            return $"{filePath}|{containingType}|{methodName}|{startLine}";
        }
    }
}
