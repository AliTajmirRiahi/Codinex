using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.References.Providers.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Document = Microsoft.CodeAnalysis.Document;
using Project = Microsoft.CodeAnalysis.Project;

namespace Codify.VisualStudio.References.Providers
{
    public sealed class ClassReferenceProvider(IVisualStudioServices visualStudio)
        : RoslynReferenceProviderBase(visualStudio)
    {
        protected override Task<IReadOnlyList<ReferenceItem>> ExtractReferencesAsync(Project project, Document document)
        {
            return ExtractClassesFromDocumentAsync(
                project,
                document);
        }


        // Fully static implementation ensures the generated async state machine class does not need to reference 'this'
        private static async Task<IReadOnlyList<ReferenceItem>> ExtractClassesFromDocumentAsync(
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

            var classDeclarations = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();
            results.AddRange(classDeclarations.Select(classDeclaration => BuildClassReferenceItem(project, document, sourceText, semanticModel, classDeclaration)).Where(item => item != null));

            return results;
        }

        private static ReferenceItem BuildClassReferenceItem(
            Project project,
            Document document,
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
