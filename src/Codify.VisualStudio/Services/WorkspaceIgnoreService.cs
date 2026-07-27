using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codify.VisualStudio.Interfaces;

namespace Codify.VisualStudio.Services;

/// <summary>
/// Filters generated and non-source files from workspace operations.
/// </summary>
public sealed class WorkspaceIgnoreService : IWorkspaceIgnoreService
{
    private static readonly HashSet<string> IgnoredDirectories =
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

    private static readonly HashSet<string> IgnoredExtensions =
    [
        ".db",
        ".dll",
        ".exe",
        ".pdb",
        ".cache",
        ".log"
    ];

    private static readonly string[] IgnoredFileSuffixes =
    [
        ".nuget.dgspec.json",
        ".deps.json",
        ".runtimeconfig.json",
        ".AssemblyInfo.cs",
        ".AssemblyAttributes.cs"
    ];

    /// <inheritdoc/>
    public bool ShouldIgnore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        var fileName = Path.GetFileName(filePath);

        if (IgnoredFiles.Contains(fileName))
            return true;

        var extension = Path.GetExtension(fileName);

        if (IgnoredExtensions.Contains(extension))
            return true;

        if (IgnoredFileSuffixes.Any(suffix => fileName.EndsWith(
                suffix,
                StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var directory = new DirectoryInfo(
            Path.GetDirectoryName(filePath)!);

        while (directory != null)
        {
            if (IgnoredDirectories.Contains(directory.Name))
                return true;

            directory = directory.Parent;
        }

        return false;
    }
}