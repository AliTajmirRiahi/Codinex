using Codify.Core.Abstractions;
using Codify.Core.Models;
using EnvDTE;
using Microsoft.CodeAnalysis;
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
    public sealed class FolderReferenceProvider : IReferenceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public FolderReferenceProvider(IServiceProvider serviceProvider)
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
            var processedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var project in currentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var filePath = document.FilePath;
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        continue;
                    }

                    // Extract the directory path of the document file
                    var directoryPath = Path.GetDirectoryName(filePath);
                    if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                    {
                        continue;
                    }

                    // Process the folder and its ancestor folders within the project scope
                    var currentDir = new DirectoryInfo(directoryPath);
                    var projectDir = !string.IsNullOrWhiteSpace(project.FilePath)
                        ? Path.GetDirectoryName(project.FilePath)
                        : null;

                    while (currentDir != null)
                    {
                        var currentDirFullName = currentDir.FullName;

                        // Stop climbing up if we go outside the project directory root (to keep scope relative to project)
                        if (projectDir != null && !currentDirFullName.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        if (processedFolders.Add(currentDirFullName))
                        {
                            var folderItem = BuildFolderReferenceItem(project, currentDir);
                            if (folderItem != null)
                            {
                                result.Add(folderItem);
                            }
                        }

                        currentDir = currentDir.Parent;
                    }
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

        private static ReferenceItem BuildFolderReferenceItem(Microsoft.CodeAnalysis.Project project, DirectoryInfo directoryInfo)
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
