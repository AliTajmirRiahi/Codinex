namespace Codinex.Core.Models.References
{
    /// <summary>
    /// Builds deterministic <see cref="ReferenceItem.Id"/> values so the same file or symbol
    /// always maps to the same id across rebuilds. This is what makes incremental add/remove/update
    /// events possible: without a stable id there is nothing to diff a new snapshot against.
    /// </summary>
    public static class ReferenceIdBuilder
    {
        public static string BuildFileId(string filePath)
        {
            return $"file:{NormalizePath(filePath)}";
        }

        public static string BuildSymbolId(
            ReferenceKind kind,
            string filePath,
            string containerName,
            string signature)
        {
            return $"{kind}:{NormalizePath(filePath)}|{(containerName ?? string.Empty).Trim()}|{(signature ?? string.Empty).Trim()}";
        }

        private static string NormalizePath(string filePath)
        {
            return (filePath ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
