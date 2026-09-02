using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Storage.Interfaces;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Services;

/// <summary>
/// Filters generated and non-source files from workspace operations.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
public sealed class WorkspaceIgnoreService(
    IFileSystem fileSystem,
    IWorkspaceSettingsManager workspaceSettingsManager) : IWorkspaceIgnoreService
{
    private static readonly string[] DefaultIgnoredDirectories =
    [
        ".git",
        ".vs",
        "bin",
        "obj",
        "node_modules",
        "packages",
        "TestResults"
    ];

    private static readonly HashSet<string> IgnoredFiles =
    [
        "project.assets.json"
    ];

    private HashSet<string> IgnoredDirectories
    {
        get
        {
            var directories = new HashSet<string>(DefaultIgnoredDirectories, StringComparer.OrdinalIgnoreCase);

            foreach (var entry in ParseList(workspaceSettingsManager.Settings?.ExcludeDirectories))
                directories.Add(entry);

            return directories;
        }
    }

    private HashSet<string> IgnoredExtensions =>
        new(ParseList(workspaceSettingsManager.Settings?.IgnoredExtensions), StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<string> IgnoredFileSuffixes =>
        ParseList(workspaceSettingsManager.Settings?.IgnoredFileSuffixes).ToList();

    private IReadOnlyList<string> ExcludedFilePatterns =>
        ParseList(workspaceSettingsManager.Settings?.ExcludeFiles).ToList();

    /// <inheritdoc/>
    public bool ShouldIgnore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        var fileName = fileSystem.Path.GetFileName(filePath);

        if (IgnoredFiles.Contains(fileName))
            return true;

        var extension = fileSystem.Path.GetExtension(fileName);

        if (IgnoredExtensions.Contains(extension))
            return true;

        if (IgnoredFileSuffixes.Any(suffix => fileName.EndsWith(
                suffix,
                StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (ExcludedFilePatterns.Any(pattern => MatchesFilePattern(fileName, pattern)))
            return true;

        var current = fileSystem.Directory.Exists(filePath)
            ? fileSystem.DirectoryInfo.New(filePath)
            : fileSystem.DirectoryInfo.New(fileSystem.Path.GetDirectoryName(filePath)!);

        var ignoredDirectories = IgnoredDirectories;

        while (current != null)
        {
            if (ignoredDirectories.Contains(current.Name))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private static bool MatchesFilePattern(string fileName, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        // A pattern made up only of wildcard / dot / separator characters (e.g. "*", "*.*",
        // "**") expands to a match-everything regex and would silently hide the entire
        // workspace. Never let one exclude a file - treat it as a no-op.
        if (pattern.Trim('*', '?', '.', ' ', '/').Length == 0)
            return false;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
    }

    private static IEnumerable<string> ParseList(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(entry => entry.Trim())
                .Where(entry => entry.Length > 0);
}
