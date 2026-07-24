using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Workspace.Providers
{
#pragma warning disable VSTHRD010

    /// <summary>
    /// Provides information about currently open documents.
    /// </summary>
    public sealed class OpenDocumentsProvider(
        IVisualStudioServices visualStudio,
        IWorkspaceContext workspaceContext,
        IWorkspaceFileService workspaceFileService,
        IUiThreadDispatcher uiThreadDispatcher)
        : VsServiceBase(visualStudio), IOpenDocumentsProvider
    {
        public async Task<IReadOnlyList<ReferenceItem>> GetOpenDocumentsAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await uiThreadDispatcher.SwitchToMainThreadAsync();

            var result = new List<ReferenceItem>();

            var dte = await GetDteAsync();

            if (dte?.Solution is not { IsOpen: true })
            {
                return result;
            }

            foreach (var document in dte.Documents.Cast<Document>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(document.FullName))
                {
                    continue;
                }

                if (!workspaceFileService.Exists(document.FullName))
                {
                    continue;
                }

                result.Add(CreateReferenceItem(document));
            }

            return result
                .GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
        }

        private ReferenceItem CreateReferenceItem(
            Document document)
        {
            return new ReferenceItem
            {
                Id = $"open:{Guid.NewGuid()}",
                Name = Path.GetFileName(document.FullName),
                Description = "Open document",
                Type = ReferenceKind.OpenDocuments,
                Icon = string.Empty,
                Value = document.FullName,
                Metadata = new ReferenceMetadata
                {
                    FilePath = document.FullName,
                    ContainerName = Path.GetDirectoryName(document.FullName),
                    ProjectName = workspaceContext.SolutionName
                }
            };
        }
    }

#pragma warning restore VSTHRD010
}