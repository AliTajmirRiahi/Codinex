using System;

namespace Codify.VisualStudio.Extensions;

internal static class PathExtensions
{
    public static string GetRelativePath(string relativeTo, string path)
    {
        var fromUri = new Uri(AppendDirectorySeparatorChar(relativeTo));
        var toUri = new Uri(path);

        var relativeUri = fromUri.MakeRelativeUri(toUri);

        return Uri.UnescapeDataString(relativeUri.ToString())
            .Replace('/', '\\');
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        if (!path.EndsWith("\\"))
            return path + "\\";

        return path;
    }
}
