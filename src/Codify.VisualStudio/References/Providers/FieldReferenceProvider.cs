using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.References.Providers.Base;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Document = Microsoft.CodeAnalysis.Document;
using Project = Microsoft.CodeAnalysis.Project;

namespace Codify.VisualStudio.References.Providers
{
    public sealed class FieldReferenceProvider(IVisualStudioServices visualStudio, IUiThreadDispatcher uiThreadDispatcher)
        : RoslynReferenceProviderBase(visualStudio, uiThreadDispatcher)
    {
        protected override Task<IReadOnlyList<ReferenceItem>> ExtractReferencesAsync(Project project, Document document)
        {
            return ExtractFieldsAndPropertiesFromDocumentAsync(
                project,
                document);
        }

        // Fully static implementation ensures the generated async state machine class does not need to reference 'this'
        private static async Task<IReadOnlyList<ReferenceItem>> ExtractFieldsAndPropertiesFromDocumentAsync(
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

            // 1. Process Class/Struct Fields
            var fieldDeclarations = syntaxRoot.DescendantNodes().OfType<FieldDeclarationSyntax>();
            results.AddRange(fieldDeclarations.SelectMany(fieldDeclaration => fieldDeclaration.Declaration.Variables, (fieldDeclaration, variable) => BuildFieldReferenceItem(project, document, sourceText, semanticModel, fieldDeclaration, variable)).Where(item => item != null));

            // 2. Process Class/Struct Properties
            var propertyDeclarations = syntaxRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            results.AddRange(propertyDeclarations.Select(propertyDeclaration => BuildPropertyReferenceItem(project, document, sourceText, semanticModel, propertyDeclaration)).Where(item => item != null));

            return results;
        }

        private static ReferenceItem BuildFieldReferenceItem(
            Project project,
            Document document,
            SourceText sourceText,
            SemanticModel semanticModel,
            FieldDeclarationSyntax fieldDeclaration,
            VariableDeclaratorSyntax variable)
        {
            var symbol = semanticModel?.GetDeclaredSymbol(variable) as IFieldSymbol;
            var fieldName = variable.Identifier.ValueText;

            var containingType = symbol?.ContainingType?.ToDisplayString()
                                 ?? GetContainingTypeName(fieldDeclaration);

            var signature = symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                           ?? BuildFallbackFieldSignature(fieldDeclaration, variable);

            var body = variable.Initializer != null
                ? variable.Initializer.NormalizeWhitespace().ToFullString()
                : string.Empty;

            var fullCode = fieldDeclaration.NormalizeWhitespace().ToFullString();

            var lineSpan = sourceText.Lines.GetLinePositionSpan(variable.Span);
            var startLine = lineSpan.Start.Line + 1;
            var endLine = lineSpan.End.Line + 1;

            var filePath = document.FilePath ?? string.Empty;
            var fileName = Path.GetFileName(filePath);
            var projectName = project?.Name ?? string.Empty;

            return new ReferenceItem
            {
                Id = $"file:{Guid.NewGuid()}",
                Name = fieldName,
                Description = fileName,
                Type = ReferenceKind.Field,
                Icon = "symbols/symbol-field", // Placeholder for actual icon representation
                Color = "--vs-viz-surface-strong-blue-medium-color",
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

        private static ReferenceItem BuildPropertyReferenceItem(
            Project project,
            Document document,
            SourceText sourceText,
            SemanticModel semanticModel,
            PropertyDeclarationSyntax propertyDeclaration)
        {
            var symbol = semanticModel?.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;
            var propertyName = propertyDeclaration.Identifier.ValueText;

            var containingType = symbol?.ContainingType?.ToDisplayString()
                                 ?? GetContainingTypeName(propertyDeclaration);

            var signature = symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                           ?? BuildFallbackPropertySignature(propertyDeclaration);

            var body = GetPropertyBody(propertyDeclaration);
            var fullCode = propertyDeclaration.NormalizeWhitespace().ToFullString();

            var lineSpan = sourceText.Lines.GetLinePositionSpan(propertyDeclaration.Span);
            var startLine = lineSpan.Start.Line + 1;
            var endLine = lineSpan.End.Line + 1;

            var filePath = document.FilePath ?? string.Empty;
            var fileName = Path.GetFileName(filePath);
            var projectName = project?.Name ?? string.Empty;

            return new ReferenceItem
            {
                Id = $"file:{Guid.NewGuid()}",
                Name = propertyName,
                Description = fileName,
                Type = ReferenceKind.Field, // Mapping property to Field reference kind or specific kind if available
                Icon = "symbols/symbol-property", // Placeholder for actual icon representation
                Color = "--vs-viz-surface-strong-blue-medium-color",
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

        private static string GetContainingTypeName(SyntaxNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TypeDeclarationSyntax typeDeclaration)
                {
                    return typeDeclaration.Identifier.ValueText;
                }
                current = current.Parent;
            }
            return "UnknownType";
        }

        private static string BuildFallbackFieldSignature(FieldDeclarationSyntax fieldDeclaration, VariableDeclaratorSyntax variable)
        {
            var modifiers = fieldDeclaration.Modifiers.ToString();
            var type = fieldDeclaration.Declaration.Type.ToString();
            var name = variable.Identifier.ValueText;

            return string.IsNullOrWhiteSpace(modifiers)
                ? $"{type} {name}"
                : $"{modifiers} {type} {name}";
        }

        private static string BuildFallbackPropertySignature(PropertyDeclarationSyntax propertyDeclaration)
        {
            var modifiers = propertyDeclaration.Modifiers.ToString();
            var type = propertyDeclaration.Type.ToString();
            var name = propertyDeclaration.Identifier.ValueText;

            return string.IsNullOrWhiteSpace(modifiers)
                ? $"{type} {name}"
                : $"{modifiers} {type} {name}";
        }

        private static string GetPropertyBody(PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.AccessorList != null)
            {
                return propertyDeclaration.AccessorList.NormalizeWhitespace().ToFullString();
            }

            if (propertyDeclaration.ExpressionBody != null)
            {
                return "=> " + propertyDeclaration.ExpressionBody.Expression.NormalizeWhitespace().ToFullString() + ";";
            }

            return string.Empty;
        }
    }
}
