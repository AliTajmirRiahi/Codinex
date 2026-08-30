using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Storage.Interfaces;

namespace Codinex.VisualStudio.Services;

/// <summary>
/// On solution open, reads the workspace's version-control ignore files
/// (<c>.gitignore</c>, <c>.git/info/exclude</c>, <c>.tfignore</c>) and folds the
/// directory / file-name patterns they list into <see cref="Storage.Models.WorkspaceSettings"/>
/// (<c>ExcludeDirectories</c> / <c>ExcludeFiles</c>), which <see cref="WorkspaceIgnoreService"/>
/// already consults. This keeps generated output (<c>dist</c>, <c>build</c>, bundles,
/// source maps, …) out of workspace search and tool results without the user hand-maintaining
/// a second exclude list.
///
/// Deliberately conservative: only root-level ignore files are read, negation
/// (<c>!pattern</c>) rules are skipped (the flat settings model cannot express an
/// un-ignore), and merging is a case-insensitive union that only persists when it
/// actually adds something - so it is safe to run on every launch.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Infrastructure)]
public sealed class WorkspaceIgnoreImporter(
    IFileSystem fileSystem,
    IWorkspaceContext workspaceContext,
    IWorkspaceSettingsManager workspaceSettingsManager) : IStartupTask
{
    private const int MaxImportedPatterns = 200;

    private static readonly string[] IgnoreFileRelativePaths =
    [
        ".gitignore",
        ".git/info/exclude",
        ".tfignore"
    ];

    private static readonly HashSet<string> KnownDotDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".vs", ".git", ".idea", ".vscode", ".hg", ".svn", ".gradle", ".nuget"
    };

    public async Task StartAsync()
    {
        try
        {
            var root = workspaceContext.SolutionDirectory;

            if (string.IsNullOrWhiteSpace(root) || !fileSystem.Directory.Exists(root))
            {
                return;
            }

            var settings = workspaceSettingsManager.Settings;

            if (settings == null)
            {
                return;
            }

            var directories = ToSet(settings.ExcludeDirectories);
            var files = ToSet(settings.ExcludeFiles);

            var directoriesBefore = directories.Count;
            var filesBefore = files.Count;

            var imported = 0;

            foreach (var relativePath in IgnoreFileRelativePaths)
            {
                if (imported >= MaxImportedPatterns)
                {
                    break;
                }

                var fullPath = fileSystem.Path.Combine(
                    root,
                    relativePath.Replace('/', fileSystem.Path.DirectorySeparatorChar));

                if (!fileSystem.File.Exists(fullPath))
                {
                    continue;
                }

                foreach (var line in ReadLinesSafe(fullPath))
                {
                    if (imported >= MaxImportedPatterns)
                    {
                        break;
                    }

                    if (!TryClassify(line, out var value, out var isDirectory))
                    {
                        continue;
                    }

                    var target = isDirectory ? directories : files;

                    if (target.Add(value))
                    {
                        imported++;
                    }
                }
            }

            if (directories.Count == directoriesBefore && files.Count == filesBefore)
            {
                return;
            }

            settings.ExcludeDirectories = string.Join(";", directories);
            settings.ExcludeFiles = string.Join(";", files);

            await workspaceSettingsManager.SaveAsync(settings);
        }
        catch
        {
            // Startup boundary: importing ignore rules is best-effort and must never
            // block the rest of the extension from initializing.
        }
    }

    private IEnumerable<string> ReadLinesSafe(string fullPath)
    {
        try
        {
            return fileSystem.File.ReadAllLines(fullPath);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// Maps one ignore-file line to a bare directory name or file-name pattern, or rejects it.
    /// Path-anchored globs are reduced to their last segment - a coarse but safe approximation
    /// for a flat name/pattern exclude list.
    /// </summary>
    private static bool TryClassify(string rawLine, out string value, out bool isDirectory)
    {
        value = null;
        isDirectory = false;

        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return false;
        }

        var line = rawLine.Trim();

        // Comments and negation (un-ignore) rules have no representation here.
        if (line.StartsWith("#") || line.StartsWith("!"))
        {
            return false;
        }

        line = line.Replace('\\', '/');

        var endsWithSlash = line.EndsWith("/");

        line = line.Trim('/');

        if (line.Length == 0)
        {
            return false;
        }

        var lastSlash = line.LastIndexOf('/');

        var lastSegment = lastSlash >= 0
            ? line.Substring(lastSlash + 1)
            : line;

        if (lastSegment.Length == 0 || lastSegment == "**")
        {
            return false;
        }

        var hasWildcard = lastSegment.Contains('*') || lastSegment.Contains('?');

        if (endsWithSlash)
        {
            // Explicit directory marker.
            if (hasWildcard)
            {
                return false;
            }

            value = lastSegment;
            isDirectory = true;
            return true;
        }

        if (hasWildcard)
        {
            value = lastSegment;
            isDirectory = false;
            return true;
        }

        // No wildcard and no trailing slash: a plain name.
        value = lastSegment;

        if (lastSegment.StartsWith("."))
        {
            // Leading-dot name: a few well-known ones are tool directories, the rest
            // (".env", ".env.local", ".DS_Store") are treated as files.
            isDirectory = KnownDotDirectories.Contains(lastSegment);
            return true;
        }

        // "styles.css" / "secrets.json" -> file; "dist" / "build" / "coverage" -> directory.
        isDirectory = !lastSegment.Contains('.');
        return true;
    }

    private static HashSet<string> ToSet(string semicolonList)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(semicolonList))
        {
            return set;
        }

        foreach (var entry in semicolonList.Split([';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = entry.Trim();

            if (trimmed.Length > 0)
            {
                set.Add(trimmed);
            }
        }

        return set;
    }
}
