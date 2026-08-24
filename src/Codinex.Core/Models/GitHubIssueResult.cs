namespace Codinex.Core.Models
{
    public sealed class GitHubIssueResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public string IssueUrl { get; set; }

        public static GitHubIssueResult Ok(string issueUrl) =>
            new() { Success = true, IssueUrl = issueUrl };

        public static GitHubIssueResult Failed(string message) =>
            new() { Success = false, Message = message };
    }
}
