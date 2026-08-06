using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.VisualStudio.Extensions;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models;
using Codinex.VisualStudio.Tools.BuiltIn.Files;

namespace Codinex.VisualStudio.Services;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class SourceFileElementService(
    IWorkspaceContext workspaceContext,
    IWorkspaceFileService workspaceFileService,
    IWorkspaceSearchService workspaceSearchService)
    : ISourceFileElementService
{
    public IReadOnlyList<string> SupportedExtensions => SourceFileElementParser.SupportedExtensions;

    public bool IsSupported(string path)
    {
        return SourceFileElementParser.IsSupported(path);
    }

    public string GetLanguage(string path)
    {
        return SourceFileElementParser.GetLanguage(path);
    }

    public SourceFileOutline GetFileOutline(
        string fullPath,
        string relativePath = null)
    {
        relativePath = string.IsNullOrWhiteSpace(relativePath)
            ? GetWorkspaceRelativePath(fullPath)
            : relativePath;

        if (!IsSupported(relativePath))
        {
            return new SourceFileOutline
            {
                File = SourceFileElementParser.NormalizePath(relativePath),
                Language = GetLanguage(relativePath),
                Elements = []
            };
        }

        var content = workspaceFileService.Read(fullPath);
        var elements = SourceFileElementParser.Parse(relativePath, content)
            .OrderBy(e => e.Order)
            .ToArray();

        return new SourceFileOutline
        {
            File = SourceFileElementParser.NormalizePath(relativePath),
            Language = GetLanguage(relativePath),
            Elements = elements
        };
    }

    public SourceFileElement FindElement(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            return null;
        }

        var files = SupportedExtensions
            .SelectMany(extension => workspaceSearchService.FindByExtension(extension))
            .Where(file => IsSupported(file.RelativePath))
            .GroupBy(file => file.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());

        foreach (var file in files)
        {
            try
            {
                var element = GetFileOutline(file.FullPath, file.RelativePath)
                    .Elements
                    .FirstOrDefault(e => e.Id.Equals(elementId, StringComparison.Ordinal));

                if (element != null)
                {
                    return element;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Skip files that cannot be read.
            }
        }

        return null;
    }

    private string GetWorkspaceRelativePath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return string.Empty;
        }

        var root = workspaceContext.SolutionDirectory;

        if (string.IsNullOrWhiteSpace(root))
        {
            return fullPath;
        }

        try
        {
            var normalizedRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedPath = Path.GetFullPath(fullPath);

            if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return PathExtensions.GetRelativePath(root, fullPath);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Fall back to the provided path.
        }

        return fullPath;
    }
}
