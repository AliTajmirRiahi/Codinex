using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Services;

/// <summary>
/// Filters generated and non-source files from workspace operations.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
public sealed class WorkspaceIgnoreService(IFileSystem fileSystem) : IWorkspaceIgnoreService
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

        var current = fileSystem.Directory.Exists(filePath)
            ? fileSystem.DirectoryInfo.New(filePath)
            : fileSystem.DirectoryInfo.New(fileSystem.Path.GetDirectoryName(filePath)!);

        while (current != null)
        {
            if (IgnoredDirectories.Contains(current.Name))
                return true;

            current = current.Parent;
        }

        return false;
    }
}