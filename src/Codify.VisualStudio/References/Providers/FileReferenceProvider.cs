using Codify.Core.Interfaces;
using Codify.Core.Models;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Internal;

namespace Codify.VisualStudio.References.Providers
{
    public class FileReferenceProvider(IVisualStudioServices visualStudio, IWorkspaceContext workspaceContext , IFileSystem fileSystem)
        : VsServiceBase(visualStudio), IReferenceProvider, IActiveDocumentProvider
    {
        private readonly IWorkspaceContext _workspaceContext = workspaceContext;
        private readonly IFileSystem _fileSystem = fileSystem;

        public sealed class FileIconInfo
        {
            public string Id { get; set; }
            public string Icon { get; set; }
            public string Description { get; set; }
        }

        private static readonly Dictionary<string, FileIconInfo> FileIcons =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Programming Languages
                [".cs"] = new FileIconInfo { Id = ".cs", Icon = "fileTypes/file_type_csharp", Description = "C# source file" },
                [".vb"] = new FileIconInfo { Id = ".vb", Icon = "fileTypes/file_type_vb", Description = "Visual Basic source file" },
                [".cpp"] = new FileIconInfo { Id = ".cpp", Icon = "fileTypes/file_type_cpp", Description = "C++ source file" },
                [".c"] = new FileIconInfo { Id = ".c", Icon = "fileTypes/file_type_c", Description = "C source file" },
                [".h"] = new FileIconInfo { Id = ".h", Icon = "fileTypes/file_type_h", Description = "C/C++ header file" },
                [".hpp"] = new FileIconInfo { Id = ".hpp", Icon = "fileTypes/file_type_hpp", Description = "C++ header file" },
                [".cc"] = new FileIconInfo { Id = ".cc", Icon = "fileTypes/file_type_c", Description = "C++ source file" },
                [".cxx"] = new FileIconInfo { Id = ".cxx", Icon = "fileTypes/file_type_c", Description = "C++ source file" },
                [".fs"] = new FileIconInfo { Id = ".fs", Icon = "fileTypes/file_type_fsharp2", Description = "F# source file" },
                [".fsi"] = new FileIconInfo { Id = ".fsi", Icon = "fileTypes/file_type_fsharp2", Description = "F# signature file" },
                [".fsx"] = new FileIconInfo { Id = ".fsx", Icon = "fileTypes/file_type_fsharp2", Description = "F# script file" },
                [".py"] = new FileIconInfo { Id = ".py", Icon = "fileTypes/file_type_python", Description = "Python source file" },
                [".pyw"] = new FileIconInfo { Id = ".pyw", Icon = "fileTypes/file_type_python", Description = "Python windowed script" },
                [".java"] = new FileIconInfo { Id = ".java", Icon = "fileTypes/file_type_java", Description = "Java source file" },
                [".go"] = new FileIconInfo { Id = ".go", Icon = "fileTypes/file_type_go", Description = "Go source file" },
                [".rs"] = new FileIconInfo { Id = ".rs", Icon = "fileTypes/file_type_rs", Description = "Rust source file" },
                [".dll"] = new FileIconInfo { Id = ".dll", Icon = "fileTypes/file_type_dll", Description = "Dynamic Link Library" },
                [".exe"] = new FileIconInfo { Id = ".exe", Icon = "fileTypes/file_type_exe", Description = "Executable file" },

                // Web & Scripting
                [".js"] = new FileIconInfo { Id = ".js", Icon = "fileTypes/file_type_js", Description = "JavaScript file" },
                [".jsx"] = new FileIconInfo { Id = ".jsx", Icon = "fileTypes/file_type_reactjs", Description = "React JSX file" },
                [".ts"] = new FileIconInfo { Id = ".ts", Icon = "fileTypes/file_type_ts", Description = "TypeScript file" },
                [".tsx"] = new FileIconInfo { Id = ".tsx", Icon = "fileTypes/file_type_reactjs", Description = "React TSX file" },
                [".html"] = new FileIconInfo { Id = ".html", Icon = "fileTypes/file_type_html", Description = "HTML file" },
                [".htm"] = new FileIconInfo { Id = ".htm", Icon = "fileTypes/file_type_html", Description = "HTML file" },
                [".css"] = new FileIconInfo { Id = ".css", Icon = "fileTypes/file_type_css", Description = "CSS stylesheet" },
                [".scss"] = new FileIconInfo { Id = ".scss", Icon = "fileTypes/file_type_scss", Description = "SCSS stylesheet" },
                [".sass"] = new FileIconInfo { Id = ".sass", Icon = "fileTypes/file_type_sass", Description = "Sass stylesheet" },
                [".less"] = new FileIconInfo { Id = ".less", Icon = "fileTypes/file_type_less", Description = "LESS stylesheet" },
                [".php"] = new FileIconInfo { Id = ".php", Icon = "fileTypes/file_type_php", Description = "PHP file" },
                [".sql"] = new FileIconInfo { Id = ".sql", Icon = "fileTypes/file_type_sql", Description = "SQL script" },

                // Configuration & Metadata
                [".json"] = new FileIconInfo { Id = ".json", Icon = "fileTypes/file_type_json", Description = "JSON file" },
                [".xml"] = new FileIconInfo { Id = ".xml", Icon = "fileTypes/file_type_xml", Description = "XML file" },
                [".resx"] = new FileIconInfo { Id = ".resx", Icon = "fileTypes/file_type_xml", Description = "XML Resource file" },
                [".xaml"] = new FileIconInfo { Id = ".xaml", Icon = "fileTypes/file_type_xaml", Description = "XAML file" },
                [".xsd"] = new FileIconInfo { Id = ".xsd", Icon = "fileTypes/file_type_xml", Description = "XML schema file" },
                [".config"] = new FileIconInfo { Id = ".config", Icon = "fileTypes/file_type_config", Description = "Configuration file" },
                [".props"] = new FileIconInfo { Id = ".props", Icon = "fileTypes/file_type_xml", Description = "MSBuild props file" },
                [".targets"] = new FileIconInfo { Id = ".targets", Icon = "fileTypes/file_type_xml", Description = "MSBuild targets file" },
                [".yaml"] = new FileIconInfo { Id = ".yaml", Icon = "fileTypes/file_type_yaml", Description = "YAML file" },
                [".yml"] = new FileIconInfo { Id = ".yml", Icon = "fileTypes/file_type_yaml", Description = "YAML file" },
                [".csproj"] = new FileIconInfo { Id = ".csproj", Icon = "fileTypes/file_type_csproj", Description = "C# project file" },
                [".vbproj"] = new FileIconInfo { Id = ".vbproj", Icon = "fileTypes/file_type_vbproj", Description = "Visual Basic project file" },
                [".fsproj"] = new FileIconInfo { Id = ".fsproj", Icon = "fileTypes/file_type_fsproj", Description = "F# project file" },
                [".sln"] = new FileIconInfo { Id = ".sln", Icon = "fileTypes/file_type_sln", Description = "Visual Studio solution file" },
                [".editorconfig"] = new FileIconInfo { Id = ".editorconfig", Icon = "fileTypes/file_type_editorconfig", Description = "EditorConfig file" },
                [".gitignore"] = new FileIconInfo { Id = ".gitignore", Icon = "fileTypes/file_type_gitignore", Description = "Git ignore file" },
                [".gitattributes"] = new FileIconInfo { Id = ".gitattributes", Icon = "fileTypes/file_type_gitignore", Description = "Git attributes file" },
                [".env"] = new FileIconInfo { Id = ".env", Icon = "fileTypes/folder_type_environments", Description = "Environment variables file" },

                // Markup & Documents
                [".md"] = new FileIconInfo { Id = ".md", Icon = "fileTypes/file_type_markdown2", Description = "Markdown file" },
                [".markdown"] = new FileIconInfo { Id = ".markdown", Icon = "fileTypes/file_type_markdown", Description = "Markdown file" },
                [".txt"] = new FileIconInfo { Id = ".txt", Icon = "fileTypes/file_type_text", Description = "Text file" },
                [".csv"] = new FileIconInfo { Id = ".csv", Icon = "fileTypes/file_type_table", Description = "CSV file" },

                // Images
                [".png"] = new FileIconInfo { Id = ".png", Icon = "fileTypes/file_type_image", Description = "PNG image" },
                [".jpg"] = new FileIconInfo { Id = ".jpg", Icon = "fileTypes/file_type_jpeg", Description = "JPEG image" },
                [".jpeg"] = new FileIconInfo { Id = ".jpeg", Icon = "fileTypes/file_type_jpeg", Description = "JPEG image" },
                [".gif"] = new FileIconInfo { Id = ".gif", Icon = "fileTypes/file_type_image", Description = "GIF image" },
                [".bmp"] = new FileIconInfo { Id = ".bmp", Icon = "fileTypes/file_type_image", Description = "Bitmap image" },
                [".svg"] = new FileIconInfo { Id = ".svg", Icon = "fileTypes/file_type_svg", Description = "SVG image" },
                [".webp"] = new FileIconInfo { Id = ".webp", Icon = "fileTypes/file_type_webpack", Description = "WebP image" },
                [".ico"] = new FileIconInfo { Id = ".ico", Icon = "fileTypes/file_type_icon", Description = "Icon file" },
                [".tif"] = new FileIconInfo { Id = ".tif", Icon = "fileTypes/file_type_image", Description = "TIFF image" },
                [".tiff"] = new FileIconInfo { Id = ".tiff", Icon = "fileTypes/file_type_image", Description = "TIFF image" },


                // Default
                ["default"] = new FileIconInfo { Id = "default", Icon = "fileTypes/default_file", Description = "File" }
            };


        public async Task<IReadOnlyList<ReferenceItem>> GetReferencesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var items = new List<ReferenceItem>();
            var dte = await GetDteAsync();

            if (dte?.Solution is not { IsOpen: true })
            {
                return items;
            }

            items.Add(await GetActiveDocumentAsync());

            // Traverse all projects in the solution
            foreach (var project in dte.Solution.Projects.Cast<Project>().Where(project => project != null))
            {
                ProcessProjectItems(project.ProjectItems, items);
            }

            return items;
        }

        public async Task<ReferenceItem> GetActiveDocumentAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDteAsync();

            if (dte?.ActiveDocument == null) return null;

            var doc = dte?.ActiveDocument;
            var filePath = doc?.FullName;

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                return await GetActiveDocumentAsync(filePath);

            return null;
        }

        public async Task<ReferenceItem> GetActiveDocumentAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var iconForFile = GetIconForFile(fileName);

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var content = string.Empty;

                try
                {
                    /* Read file content safely for metadata context */
                    content = _fileSystem.ReadAllText(filePath);
                }
                catch
                {
                    /* Fallback to empty string if file is locked or inaccessible */
                }

                return new ReferenceItem
                {
                    Id = $"file:{Guid.NewGuid()}",
                    Name = $"Active Document",
                    Description = fileName,
                    Type = ReferenceKind.File,
                    Value = filePath,
                    Icon = iconForFile.Icon,
                    Metadata = new ReferenceMetadata
                    {
                        FilePath = filePath,
                        ContainerName = Path.GetDirectoryName(filePath),
                        ProjectName = _workspaceContext.SolutionName,
                        Content = content,
                    }
                };
            }

            await Task.CompletedTask;

            return null;
        }

        private void ProcessProjectItems(ProjectItems projectItems, List<ReferenceItem> items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (projectItems == null) return;

            foreach (ProjectItem item in projectItems)
            {
                // GUID for Physical File in VS
                if (item.Kind == "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}")
                {
                    var filePath = item.FileNames[1];
                    var fileName = Path.GetFileName(filePath);
                    var iconForFile = GetIconForFile(fileName);

                    var content = string.Empty;

                    try
                    {
                        /* Read file content safely for metadata context */
                        content = _fileSystem.ReadAllText(filePath);
                    }
                    catch
                    {
                        /* Fallback to empty string if file is locked or inaccessible */
                    }

                    items.Add(new ReferenceItem
                    {
                        Id = $"file:{Guid.NewGuid()}",
                        Name = fileName,
                        Description = iconForFile.Description,
                        Type = ReferenceKind.File,
                        Icon = iconForFile.Icon,
                        Value = filePath,
                        Metadata = new ReferenceMetadata
                        {
                            FilePath = filePath,
                            ContainerName = Path.GetDirectoryName(filePath),
                            ProjectName = item.ContainingProject?.Name ?? string.Empty,
                            Content = content,
                        }
                    });
                }

                // Recursively process sub-items (folders)
                if (item.ProjectItems != null)
                {
                    ProcessProjectItems(item.ProjectItems, items);
                }

                if (item.SubProject != null)
                {
                    ProcessProjectItems(item.SubProject.ProjectItems, items);
                }
            }
        }

        private string GetRelativePath(string fullPath, string solutionPath)
        {
            try
            {
                var solutionDir = Path.GetDirectoryName(solutionPath);
                if (string.IsNullOrEmpty(solutionDir)) return fullPath;

                Uri fullUri = new Uri(fullPath);
                Uri solutionUri = new Uri(solutionDir + Path.DirectorySeparatorChar);
                return Uri.UnescapeDataString(solutionUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
            catch { return fullPath; }
        }

        private FileIconInfo GetIconForFile(string fileName)
        {
            var ext = Path.GetExtension(fileName); // No need for ToLower() here because we used StringComparer.OrdinalIgnoreCase
            return FileIcons.TryGetValue(ext, out var icon) ? icon : FileIcons["default"];
        }

    }
}
