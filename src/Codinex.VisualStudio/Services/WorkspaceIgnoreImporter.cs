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

    /// <summary>
    /// Bare directory names that are safe to exclude at any depth because they are almost
    /// always generated output. A generic word from a personal ignore section ("Files",
    /// "personal", "html", "publish") is NOT imported as a directory rule - matched by name
    /// at any depth, it would hide real source folders that happen to share the name.
    /// </summary>
    private static readonly HashSet<string> KnownGeneratedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "build", "dist", "out", "target", "coverage",
        "node_modules", "packages", "bower_components", "jspm_packages",
        "TestResults", "artifacts", "BenchmarkDotNet.Artifacts",
        ".next", ".nuxt", ".turbo", ".parcel-cache", ".cache",
        "__pycache__", ".pytest_cache", ".mypy_cache", ".tox", ".venv", "venv",
        ".gradle", ".terraform", "CMakeFiles"
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

            // Heal any match-everything patterns (e.g. a bare "*" produced by an earlier,
            // less strict import of a "dir/*" rule) that may already be persisted. Left in
            // place, one of these makes WorkspaceIgnoreService hide the entire workspace.
            var changed = directories.RemoveWhere(IsDegeneratePattern) > 0;
            changed |= files.RemoveWhere(IsDegeneratePattern) > 0;

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
                        changed = true;
                    }
                }
            }

            if (!changed)
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

        var isRootAnchored = line.StartsWith("/");
        var endsWithSlash = line.EndsWith("/");

        line = line.Trim('/');

        if (line.Length == 0)
        {
            return false;
        }

        var lastSlash = line.LastIndexOf('/');
        var isPathAnchored = isRootAnchored || lastSlash >= 0;

        var lastSegment = lastSlash >= 0
            ? line.Substring(lastSlash + 1)
            : line;

        // Reject anything that would match every name: bare "*", "**", "*.*", "?" etc.
        if (IsDegeneratePattern(lastSegment))
        {
            return false;
        }

        var hasWildcard = lastSegment.Contains('*') || lastSegment.Contains('?');

        if (endsWithSlash)
        {
            // Explicit directory marker ("build/", "/src/gen/").
            if (hasWildcard)
            {
                return false;
            }

            return TryAcceptDirectory(lastSegment, isPathAnchored, out value, ref isDirectory);
        }

        // A wildcard in a path-anchored rule ("tools/**", ".axoCover/*") does not map to a
        // flat name/pattern list - only import wildcard rules that stand on their own.
        if (hasWildcard)
        {
            if (isPathAnchored)
            {
                return false;
            }

            value = lastSegment;
            isDirectory = false;
            return true;
        }

        // No wildcard and no trailing slash: a plain name.
        if (lastSegment.StartsWith("."))
        {
            // Leading-dot name: a few well-known ones are tool directories, the rest
            // (".env", ".env.local", ".DS_Store") are treated as files.
            if (KnownDotDirectories.Contains(lastSegment))
            {
                return TryAcceptDirectory(lastSegment, isPathAnchored, out value, ref isDirectory);
            }

            value = lastSegment;
            isDirectory = false;
            return true;
        }

        // "styles.css" / "secrets.json" -> file (matched by name, low blast radius).
        if (lastSegment.Contains('.'))
        {
            value = lastSegment;
            isDirectory = false;
            return true;
        }

        // Extension-less bare word -> directory ("dist", "Files"). Only import it when it
        // is anchored to a path or a well-known generated-output name; a generic word
        // matched at any depth would hide real source folders sharing that name.
        return TryAcceptDirectory(lastSegment, isPathAnchored, out value, ref isDirectory);
    }

    private static bool TryAcceptDirectory(
        string name,
        bool isPathAnchored,
        out string value,
        ref bool isDirectory)
    {
        value = null;

        if (!isPathAnchored
            && !KnownGeneratedDirectories.Contains(name)
            && !KnownDotDirectories.Contains(name))
        {
            return false;
        }

        value = name;
        isDirectory = true;
        return true;
    }

    /// <summary>
    /// True when a pattern consists only of wildcard / separator / dot characters and would
    /// therefore match every file or directory name (e.g. "*", "**", "*.*", "?", "/").
    /// </summary>
    private static bool IsDegeneratePattern(string pattern) =>
        string.IsNullOrWhiteSpace(pattern) || pattern.Trim('*', '?', '.', ' ', '/').Length == 0;

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
