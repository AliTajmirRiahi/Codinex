using Codify.Core.Models;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.References.Providers.Base;
using Document = Microsoft.CodeAnalysis.Document;
using Project = Microsoft.CodeAnalysis.Project;

namespace Codify.VisualStudio.References.Providers
{
    public sealed class MethodReferenceProvider(IVisualStudioServices visualStudio)
        : RoslynReferenceProviderBase(visualStudio)
    {
        protected override Task<IReadOnlyList<ReferenceItem>> ExtractReferencesAsync(Project project, Document document)
        {
            return ExtractMethodsFromDocumentAsync(project, document);
        }


        // Fully static implementation ensures the generated async state machine class does not need to reference 'this'
        private static async Task<IReadOnlyList<ReferenceItem>> ExtractMethodsFromDocumentAsync(
            Project project,
            Document document)
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
            Project project,
            Document document,
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
                Id = $"file:{Guid.NewGuid()}",
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
    }
}
