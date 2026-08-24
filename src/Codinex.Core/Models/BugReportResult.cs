namespace Codinex.Core.Models
{
    public sealed class BugReportResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public static BugReportResult Ok(string message = null) =>
            new() { Success = true, Message = message };

        public static BugReportResult Failed(string message) =>
            new() { Success = false, Message = message };
    }
}
