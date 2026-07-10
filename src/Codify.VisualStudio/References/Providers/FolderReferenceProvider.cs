using Codify.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.References.Providers.Base;
using Microsoft.CodeAnalysis;

namespace Codify.VisualStudio.References.Providers
{
    public sealed class FolderReferenceProvider(IVisualStudioServices visualStudio)
        : WorkspaceReferenceProviderBase(visualStudio)
    {
        protected override Task<IReadOnlyList<ReferenceItem>> ExtractReferencesAsync(Project project)
        {
            var result = new List<ReferenceItem>();
            var processedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var document in project.Documents)
            {
                var filePath = document.FilePath;

                if (string.IsNullOrWhiteSpace(filePath))
                    continue;

                var directoryPath = Path.GetDirectoryName(filePath);

                if (string.IsNullOrWhiteSpace(directoryPath) ||
                    !Directory.Exists(directoryPath))
                    continue;

                var currentDirectory = new DirectoryInfo(directoryPath);

                var projectDirectory =
                    !string.IsNullOrWhiteSpace(project.FilePath)
                        ? Path.GetDirectoryName(project.FilePath)
                        : null;

                while (currentDirectory != null)
                {
                    if (projectDirectory != null &&
                        !currentDirectory.FullName.StartsWith(projectDirectory,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (processedFolders.Add(currentDirectory.FullName))
                    {
                        var item = BuildFolderReferenceItem(project, currentDirectory);

                        if (item != null)
                            result.Add(item);
                    }

                    currentDirectory = currentDirectory.Parent;
                }
            }

            return Task.FromResult<IReadOnlyList<ReferenceItem>>(result);
        }

        private static ReferenceItem BuildFolderReferenceItem(Project project, DirectoryInfo directoryInfo)
        {
            var folderPath = directoryInfo.FullName;
            var folderName = directoryInfo.Name;

            // Description set to the parent folder name as requested
            var parentFolderName = directoryInfo.Parent != null ? directoryInfo.Parent.Name : string.Empty;
            var projectName = project?.Name ?? string.Empty;

            return new ReferenceItem
            {
                Id = $"file:{Guid.NewGuid()}",
                Name = folderName,
                Description = $"Parent ({(string.IsNullOrWhiteSpace(parentFolderName) ? projectName : parentFolderName)})",
                Type = ReferenceKind.Folder, // Assuming ReferenceKind has a Folder type defined in Codify.Core.Models
                Icon = "folder", // Placeholder for actual icon representation
                Color = "--vs-viz-surface-gold-medium-color", // Placeholder for actual color representation
                Metadata = new ReferenceMetadata()
                {
                    FilePath = folderPath,
                    ProjectName = projectName,
                    ContainerName = parentFolderName,
                    Signature = folderName,
                    Body = string.Empty,
                    Content = folderPath,
                    StartLine = 0,
                    EndLine = 0
                }
            };
        }
    }
}
