using Codify.Core.Models;
using Codify.Infrastructure.VisualStudio.Internal;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codify.Infrastructure.References.Providers.Base;
using Document = Microsoft.CodeAnalysis.Document;
using Project = Microsoft.CodeAnalysis.Project;

namespace Codify.Infrastructure.References.Providers
{
    public sealed class InterfaceReferenceProvider : RoslynReferenceProviderBase
    {

        public InterfaceReferenceProvider(IVisualStudioServices visualStudio) : base(visualStudio)
        {
        }

        protected override Task<IReadOnlyList<ReferenceItem>> ExtractReferencesAsync(Project project, Document document)
        {
            return ExtractInterfacesFromDocumentAsync(
                project,
                document);
        }

        // Fully static implementation ensures the generated async state machine class does not need to reference 'this'
        private static async Task<IReadOnlyList<ReferenceItem>> ExtractInterfacesFromDocumentAsync(
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

            var interfaceDeclarations = syntaxRoot.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                var item = BuildInterfaceReferenceItem(
                    project,
                    document,
                    sourceText,
                    semanticModel,
                    interfaceDeclaration);

                if (item != null)
                {
                    results.Add(item);
                }
            }

            return results;
        }

        private static ReferenceItem BuildInterfaceReferenceItem(
            Project project,
            Microsoft.CodeAnalysis.Document document,
            SourceText sourceText,
            SemanticModel semanticModel,
            InterfaceDeclarationSyntax interfaceDeclaration)
        {
            var symbol = semanticModel?.GetDeclaredSymbol(interfaceDeclaration);
            var interfaceName = interfaceDeclaration.Identifier.ValueText;

            var namespaceName = symbol?.ContainingNamespace?.IsGlobalNamespace == false
                ? symbol.ContainingNamespace.ToDisplayString()
                : GetContainingNamespaceName(interfaceDeclaration);

            var containingType = symbol?.ContainingType?.ToDisplayString()
                                 ?? GetContainingTypeName(interfaceDeclaration);

            var containerName = !string.IsNullOrWhiteSpace(containingType)
                ? containingType
                : namespaceName;

            var signature = symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                           ?? BuildFallbackSignature(interfaceDeclaration);

            var body = GetInterfaceBody(interfaceDeclaration);
            var fullCode = interfaceDeclaration.NormalizeWhitespace().ToFullString();

            var lineSpan = sourceText.Lines.GetLinePositionSpan(interfaceDeclaration.Span);
            var startLine = lineSpan.Start.Line + 1;
            var endLine = lineSpan.End.Line + 1;

            var filePath = document.FilePath ?? string.Empty;
            var fileName = Path.GetFileName(filePath);
            var projectName = project?.Name ?? string.Empty;

            return new ReferenceItem
            {
                Id = $"file:{Guid.NewGuid()}",
                Name = interfaceName,
                Description = fileName,
                Type = ReferenceKind.Interface,
                Icon = "symbols/symbol-interface", // Placeholder for actual icon representation
                Color = "--vs-viz-surface-steel-blue-medium-color",
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

        private static string GetContainingNamespaceName(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            var current = interfaceDeclaration.Parent;

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

        private static string GetContainingTypeName(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            return interfaceDeclaration.Parent is TypeDeclarationSyntax parentType ? parentType.Identifier.ValueText : string.Empty;
        }

        private static string BuildFallbackSignature(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            var modifiers = interfaceDeclaration.Modifiers.ToString();
            var interfaceName = interfaceDeclaration.Identifier.ValueText;
            var typeParameters = interfaceDeclaration.TypeParameterList?.ToString() ?? string.Empty;
            var baseList = interfaceDeclaration.BaseList?.ToString() ?? string.Empty;
            var constraintClauses = interfaceDeclaration.ConstraintClauses.Any()
                ? " " + string.Join(" ", interfaceDeclaration.ConstraintClauses.Select(c => c.ToString()))
                : string.Empty;

            var prefix = string.IsNullOrWhiteSpace(modifiers)
                ? "interface"
                : $"{modifiers} interface";

            return $"{prefix} {interfaceName}{typeParameters} {baseList}{constraintClauses}".Trim();
        }

        private static string GetInterfaceBody(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            if (interfaceDeclaration.OpenBraceToken.IsMissing || interfaceDeclaration.CloseBraceToken.IsMissing)
            {
                return string.Empty;
            }

            var members = interfaceDeclaration.Members;
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
