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
    public sealed class InterfaceReferenceProvider : IReferenceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public InterfaceReferenceProvider(IServiceProvider serviceProvider)
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
                    var interfaceItems = await ExtractInterfacesFromDocumentAsync(project, document).ConfigureAwait(false);
                    result.AddRange(interfaceItems);
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
            {
                return false;
            }

            var filePath = document.FilePath;
            return !string.IsNullOrWhiteSpace(filePath) && filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        // Fully static implementation ensures the generated async state machine class does not need to reference 'this'
        private static async Task<IReadOnlyList<ReferenceItem>> ExtractInterfacesFromDocumentAsync(
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
            Microsoft.CodeAnalysis.Project project,
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
