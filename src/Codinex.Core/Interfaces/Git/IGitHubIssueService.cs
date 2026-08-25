using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.Git
{
    /// <summary>
    /// Files bug reports as GitHub issues on the configured repo.
    /// </summary>
    public interface IGitHubIssueService
    {
        Task<GitHubIssueResult> CreateIssueAsync(
            string title,
            string body,
            CancellationToken cancellationToken = default);
    }
}
