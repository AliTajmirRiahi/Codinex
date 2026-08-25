using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Git;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Models;

namespace Codinex.Infrastructure.BugReporting
{
    // https://docs.github.com/en/rest/issues/issues#create-an-issue
    [AutoDiRegister(Modules.AI, RegistrationOrder.Infrastructure)]
    public sealed class GitHubIssueService(
        IHttpService httpService,
        IJsonSerializer jsonSerializer) : IGitHubIssueService
    {
        public async Task<GitHubIssueResult> CreateIssueAsync(
            string title,
            string body,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = $"https://api.github.com/repos/{GitHubIssueOptions.Owner}/{GitHubIssueOptions.Repo}/issues";

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(
                        jsonSerializer.Serialize(new { title, body }),
                        Encoding.UTF8,
                        "application/json")
                };

                // GitHub's REST API requires a User-Agent and rejects requests without one.
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Codinex-AI", "1.0"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("token", GitHubIssueOptions.Token);

                using var response = await httpService.SendAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return GitHubIssueResult.Failed(
                        $"GitHub API returned {(int)response.StatusCode}: {responseBody}");
                }

                var json = jsonSerializer.Parse(responseBody);
                var issueUrl = json["html_url"]?.ToString();

                return GitHubIssueResult.Ok(issueUrl);
            }
            catch (Exception ex)
            {
                return GitHubIssueResult.Failed($"Failed to create GitHub issue: {ex.Message}");
            }
        }
    }
}
