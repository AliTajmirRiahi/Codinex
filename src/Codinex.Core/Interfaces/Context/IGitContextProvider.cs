using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.Context;

namespace Codinex.Core.Interfaces.Context
{
    /// <summary>
    /// Provides Git context for the current workspace.
    /// </summary>
    public interface IGitContextProvider
    {
        /// <summary>
        /// Gets the current branch and pending (uncommitted) changes.
        /// </summary>
        Task<GitContext> GetContextAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns <c>true</c> when the workspace has at least one pending (uncommitted)
        /// change. Cheaper than <see cref="GetContextAsync"/> — it only scans working-tree
        /// status and never builds diffs/patches, so it is safe to call on a short poll.
        /// </summary>
        Task<bool> HasPendingChangesAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the most recent commits (changesets), newest first.
        /// </summary>
        Task<IReadOnlyList<GitCommit>> GetCommitsAsync(
            int maxCount,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the files changed by a specific commit (changeset).
        /// </summary>
        Task<IReadOnlyList<GitFileItem>> GetChangesAsync(
            string commitSha,
            CancellationToken cancellationToken);
    }
}