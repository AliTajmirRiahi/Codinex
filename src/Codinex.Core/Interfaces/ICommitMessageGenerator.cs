using System.Threading;
using System.Threading.Tasks;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Generates a commit message from the current Git changes using the active AI provider.
    /// This is a one-shot request/response call — it never touches conversation/chat history.
    /// </summary>
    public interface ICommitMessageGenerator
    {
        /// <summary>
        /// Generates a commit message from the current Git changes.
        /// Prefers staged changes; falls back to all changes when nothing is staged.
        /// </summary>
        /// <exception cref="NoGitChangesException">There are no pending Git changes.</exception>
        Task<string> GenerateAsync(string CommitMessageSystemPrompt, CancellationToken cancellationToken);
    }
}
