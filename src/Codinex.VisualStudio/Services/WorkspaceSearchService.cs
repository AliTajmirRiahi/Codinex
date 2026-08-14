using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Storage.Interfaces;
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
        IWorkspaceIgnoreService workspaceFileFilter,
        IWorkspaceSettingsManager workspaceSettingsManager)
        : IWorkspaceSearchService
    {

        private IReadOnlyList<string> ExcludeDirectory =>
            ParseExclusionList(workspaceSettingsManager.Settings.ExcludeDirectories).ToList();

        private IReadOnlyList<string> ExcludeDFiles =>
            ParseExclusionList(workspaceSettingsManager.Settings.ExcludeFiles).ToList();

        private static IEnumerable<string> ParseExclusionList(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split([';'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Trim())
                    .Where(entry => entry.Length > 0);

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
                        if (lines[i].IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        result.Add(new WorkspaceFile
                        {
                            Name = file.Name,
                            FullPath = file.FullPath,
                            RelativePath = file.RelativePath,
                            LineNumber = i + 1,
                            Preview = lines[i],
                            Column = lines[i].IndexOf(text, StringComparison.OrdinalIgnoreCase) + 1
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
                            Preview = lines[i],
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

        // Enumerates all files in the current workspace.
        private IReadOnlyList<WorkspaceFile> EnumerateWorkspaceFiles()
        {
            var root = workspaceContext.SolutionDirectory;

            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return [];

            return workspaceFileService
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(path => !workspaceFileFilter.ShouldIgnore(path))
                .Where(path => !IsExcludedDirectory(root, path))
                .Where(path => !IsExcludedFile(path))
                .Select(path => new WorkspaceFile
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    RelativePath = PathExtensions.GetRelativePath(root, path)
                })
                .ToList();
        }

        private bool IsExcludedDirectory(string root, string fullPath)
        {
            var excludeDirectory = ExcludeDirectory;

            if (excludeDirectory.Count == 0)
                return false;

            var relativePath = PathExtensions.GetRelativePath(root, fullPath);

            var segments = relativePath.Split(
                ['\\', '/'],
                StringSplitOptions.RemoveEmptyEntries);

            return segments.Any(segment => excludeDirectory
                .Any(excluded => string.Equals(excluded, segment, StringComparison.OrdinalIgnoreCase)));
        }

        private bool IsExcludedFile(string fullPath)
        {
            var excludeFiles = ExcludeDFiles;

            if (excludeFiles.Count == 0)
                return false;

            var fileName = Path.GetFileName(fullPath);

            return excludeFiles.Any(pattern => MatchesFilePattern(fileName, pattern));
        }

        private static bool MatchesFilePattern(string fileName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return false;

            if (!pattern.Contains('*') && !pattern.Contains('?'))
                return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);

            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
        }
    }
}