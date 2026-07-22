namespace Codify.Core.Models
{
    /// <summary>
    /// Represents a compiler or analyzer diagnostic.
    /// </summary>
    public sealed class DiagnosticItem
    {
        public string Id { get; set; }

        public string ProjectName { get; set; }

        public string FilePath { get; set; }

        public int Line { get; set; }

        public int Column { get; set; }

        public DiagnosticSeverity Severity { get; set; }

        public string Message { get; set; }
    }
}