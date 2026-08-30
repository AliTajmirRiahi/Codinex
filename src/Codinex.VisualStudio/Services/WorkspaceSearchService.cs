using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Workspace;
using Codinex.VisualStudio.Extensions;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models;
using Codinex.VisualStudio.Models.Tools.SearchProject;

namespace Codinex.VisualStudio.Services
{
    [AutoDiRegister(Modules.Workspace, RegistrationOrder.Foundation)]
    public sealed class WorkspaceSearchService(
        IWorkspaceContext workspaceContext,
        IWorkspaceFileService workspaceFileService,
        IWorkspaceIgnoreService workspaceFileFilter)
        : IWorkspaceSearchService
    {
        /// <summary>
        /// Upper bound on a single match preview. A raw matched line can be the entire
        /// file when it is minified/generated (bundles, source maps, single-line JSON),
        /// which otherwise floods the conversation with hundreds of KB per result.
        /// </summary>
        private const int MaxPreviewLength = 400;

        public IReadOnlyList<WorkspaceFile> FindFiles(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            query = query.Replace('\\', '/');

            var files = EnumerateWorkspaceFiles();

            // 1. Exact relative path
            var exactPath = files
                .Where(f => f.RelativePath
                    .Replace('\\', '/')
                    .Equals(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactPath.Count > 0)
                return exactPath;

            // 2. Exact file name
            var exactName = files
                .Where(f => f.Name.Equals(
                    Path.GetFileName(query),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactName.Count > 0)
                return exactName;

            // 3. Partial relative path
            return files
                .Where(f => f.RelativePath
                    .Replace('\\', '/')
                    .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        public IReadOnlyList<WorkspaceFile> FindByExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return [];

            if (!extension.StartsWith("."))
                extension = "." + extension;

            return EnumerateWorkspaceFiles()
                .Where(f => Path.GetExtension(f.Name)
                    .Equals(extension, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public IReadOnlyList<WorkspaceFile> FindByPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return [];

            var root = workspaceContext.SolutionDirectory;

            if (string.IsNullOrWhiteSpace(root))
                return [];

            return workspaceFileService
                .EnumerateFiles(root, pattern, SearchOption.AllDirectories)
                .Select(path => new WorkspaceFile
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    RelativePath = PathExtensions.GetRelativePath(root, path)
                })
                .ToList();
        }

        public IReadOnlyList<WorkspaceFile> SearchText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            var result = new List<WorkspaceFile>();

            foreach (var file in EnumerateWorkspaceFiles())
            {
                try
                {
                    if (workspaceFileService.IsBinary(file.FullPath))
                        continue;

                    var lines = workspaceFileService.Read(file.FullPath)
                        .Split(["\r\n", "\n"], StringSplitOptions.None);

                    for (var i = 0; i < lines.Length; i++)
                    {
                        var matchIndex = lines[i].IndexOf(text, StringComparison.OrdinalIgnoreCase);

                        if (matchIndex < 0)
                            continue;

                        result.Add(new WorkspaceFile
                        {
                            Name = file.Name,
                            FullPath = file.FullPath,
                            RelativePath = file.RelativePath,
                            LineNumber = i + 1,
                            Preview = BuildPreview(lines[i], matchIndex, text.Length),
                            Column = matchIndex + 1
                        });
                    }
                }
                catch (Exception ex) when (
                    ex is IOException or UnauthorizedAccessException)
                {
                    // Skip files that cannot be read.
                    continue;
                }
            }

            return result;
        }

        public IReadOnlyList<WorkspaceFile> SearchRegex(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return [];

            Regex regex;

            try
            {
                regex = new Regex(
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch (ArgumentException)
            {
                return [];
            }

            var result = new List<WorkspaceFile>();

            foreach (var file in EnumerateWorkspaceFiles())
            {
                try
                {
                    if (workspaceFileService.IsBinary(file.FullPath))
                        continue;

                    var lines = workspaceFileService.Read(file.FullPath)
                        .Split(["\r\n", "\n"], StringSplitOptions.None);

                    for (var i = 0; i < lines.Length; i++)
                    {
                        var match = regex.Match(lines[i]);

                        if (!match.Success)
                            continue;

                        result.Add(new WorkspaceFile
                        {
                            Name = file.Name,
                            FullPath = file.FullPath,
                            RelativePath = file.RelativePath,
                            LineNumber = i + 1,
                            Preview = BuildPreview(lines[i], match.Index, match.Length),
                            Column = match.Index + 1
                        });
                    }
                }
                catch (Exception ex) when (
                    ex is IOException or UnauthorizedAccessException)
                {
                    // Skip files that cannot be read.
                    continue;
                }
            }

            return result;
        }

        public IReadOnlyList<WorkspaceFile> Search(
            string query,
            SearchProjectType type)
        {
            return type switch
            {
                SearchProjectType.FileName => FindFiles(query),
                SearchProjectType.Extension => FindByExtension(query),
                SearchProjectType.Pattern => FindByPattern(query),
                SearchProjectType.Text => SearchText(query),
                SearchProjectType.Regex => SearchRegex(query),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported search type.")
            };
        }

        /// <summary>
        /// Returns at most <see cref="MaxPreviewLength"/> characters of the matched line,
        /// centred on the match so the hit stays visible, with ellipses marking any elision.
        /// </summary>
        private static string BuildPreview(string line, int matchIndex, int matchLength)
        {
            if (string.IsNullOrEmpty(line) || line.Length <= MaxPreviewLength)
            {
                return line;
            }

            var visibleMatch = Math.Min(Math.Max(matchLength, 0), MaxPreviewLength);
            var contextBudget = MaxPreviewLength - visibleMatch;

            var start = Math.Max(0, matchIndex - (contextBudget / 2));
            var length = Math.Min(line.Length - start, MaxPreviewLength);

            var slice = line.Substring(start, length);

            if (start > 0)
            {
                slice = "…" + slice;
            }

            if (start + length < line.Length)
            {
                slice += "…";
            }

            return slice;
        }

        // Enumerates all files in the current workspace.
        private IReadOnlyList<WorkspaceFile> EnumerateWorkspaceFiles()
        {
            var root = workspaceContext.SolutionDirectory;

            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return [];

            return workspaceFileService
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(path => !workspaceFileFilter.ShouldIgnore(path))
                .Select(path => new WorkspaceFile
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    RelativePath = PathExtensions.GetRelativePath(root, path)
                })
                .ToList();
        }
    }
}