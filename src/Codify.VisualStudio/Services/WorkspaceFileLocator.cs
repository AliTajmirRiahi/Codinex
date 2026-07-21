using Codify.Core.Interfaces;
using Codify.VisualStudio.Extensions;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Codify.VisualStudio.Services
{
    public sealed class WorkspaceFileLocator(
        IWorkspaceContext workspaceContext)
        : IWorkspaceFileLocator
    {
        public IReadOnlyList<WorkspaceFile> Find(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var root = workspaceContext.SolutionDirectory;

            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return [];

            query = query.Replace('\\', '/');

            var files = Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new WorkspaceFile
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    RelativePath = PathExtensions.GetRelativePath(root, path)
                });

            // Exact file name
            var exact = files
                .Where(f => f.Name.Equals(
                    Path.GetFileName(query),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exact.Count > 0)
                return exact;

            // Relative path contains
            return files
                .Where(f => f.RelativePath.Replace('\\', '/')
                    .Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}