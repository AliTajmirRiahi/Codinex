using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Interfaces.Services;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Core.Models;
using LibGit2Sharp;

namespace Codinex.VisualStudio.Workspace.Providers
{
    /// <summary>
    /// Provides Git status, commit history, and changeset details for the current workspace.
    /// </summary>
    [AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
    public sealed class GitContextProvider(
        IWorkspaceContext workspaceContext,
        IUiThreadDispatcher uiThreadDispatcher)
        : IGitContextProvider
    {
        public async Task<GitContext> GetContextAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var solutionDirectory = await GetSolutionDirectoryAsync();

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var repo = OpenRepository(solutionDirectory);

                if (repo == null)
                {
                    return new GitContext
                    {
                        BranchName = null,
                        Files = Array.Empty<GitFileItem>()
                    };
                }

                var stagedPatch = repo.Head?.Tip != null
                    ? repo.Diff.Compare<Patch>(repo.Head.Tip.Tree, DiffTargets.Index)
                    : null;

                var workdirPatch = repo.Diff.Compare<Patch>(paths: null, includeUntracked: true);

                var stagedLookup = BuildPatchLookup(stagedPatch);
                var workdirLookup = BuildPatchLookup(workdirPatch);

                var files = new List<GitFileItem>();

                foreach (var entry in repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false }))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = CreateFileItem(entry, stagedLookup, workdirLookup);

                    if (item != null)
                    {
                        files.Add(item);
                    }
                }

                return new GitContext
                {
                    BranchName = repo.Head?.FriendlyName,
                    Files = files
                };
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<GitCommit>> GetCommitsAsync(
            int maxCount,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var solutionDirectory = await GetSolutionDirectoryAsync();

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var repo = OpenRepository(solutionDirectory);

                if (repo?.Head?.Tip == null)
                {
                    return (IReadOnlyList<GitCommit>)Array.Empty<GitCommit>();
                }

                var commits = new List<GitCommit>();

                foreach (var commit in repo.Commits.Take(maxCount))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var parentTree = commit.Parents.FirstOrDefault()?.Tree;
                    var patch = repo.Diff.Compare<Patch>(parentTree, commit.Tree);

                    commits.Add(new GitCommit
                    {
                        Sha = commit.Sha,
                        ShortSha = commit.Sha.Substring(0, Math.Min(7, commit.Sha.Length)),
                        AuthorName = commit.Author.Name,
                        AuthorEmail = commit.Author.Email,
                        Date = commit.Author.When,
                        Message = commit.MessageShort,
                        LinesAdded = patch.LinesAdded,
                        LinesDeleted = patch.LinesDeleted
                    });
                }

                return (IReadOnlyList<GitCommit>)commits;
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<GitFileItem>> GetChangesAsync(
            string commitSha,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(commitSha))
            {
                throw new ArgumentException("Commit SHA must be provided.", nameof(commitSha));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var solutionDirectory = await GetSolutionDirectoryAsync();

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var repo = OpenRepository(solutionDirectory);

                var commit = repo?.Lookup<Commit>(commitSha);

                if (commit == null)
                {
                    return (IReadOnlyList<GitFileItem>)Array.Empty<GitFileItem>();
                }

                var parentTree = commit.Parents.FirstOrDefault()?.Tree;

                var changes = repo.Diff.Compare<Patch>(parentTree, commit.Tree);

                var files = new List<GitFileItem>();

                foreach (var change in changes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var status = MapChangeKind(change.Status);

                    if (status == null)
                    {
                        continue;
                    }

                    files.Add(new GitFileItem
                    {
                        Path = change.Path,
                        Status = status.Value,
                        IsStaged = false,
                        LinesAdded = change.LinesAdded,
                        LinesDeleted = change.LinesDeleted,
                        Diff = change.Patch
                    });
                }

                return (IReadOnlyList<GitFileItem>)files;
            }, cancellationToken);
        }

        private async Task<string> GetSolutionDirectoryAsync()
        {
            await uiThreadDispatcher.SwitchToMainThreadAsync();

            return workspaceContext.SolutionDirectory;
        }

        private static Repository OpenRepository(string solutionDirectory)
        {
            if (string.IsNullOrWhiteSpace(solutionDirectory))
            {
                return null;
            }

            var gitDirectory = Repository.Discover(solutionDirectory);

            return gitDirectory == null ? null : new Repository(gitDirectory);
        }

        private static GitFileItem CreateFileItem(
            StatusEntry entry,
            IReadOnlyDictionary<string, PatchEntryChanges> stagedLookup,
            IReadOnlyDictionary<string, PatchEntryChanges> workdirLookup)
        {
            var (status, isStaged) = MapFileStatus(entry.State);

            if (status == null)
            {
                return null;
            }

            var lookup = isStaged ? stagedLookup : workdirLookup;

            var linesAdded = 0;
            var linesDeleted = 0;
            string diff = null;

            if (lookup != null && lookup.TryGetValue(entry.FilePath, out var change))
            {
                linesAdded = change.LinesAdded;
                linesDeleted = change.LinesDeleted;
                diff = change.Patch;
            }

            return new GitFileItem
            {
                Path = entry.FilePath,
                Status = status.Value,
                IsStaged = isStaged,
                LinesAdded = linesAdded,
                LinesDeleted = linesDeleted,
                Diff = diff
            };
        }

        private static IReadOnlyDictionary<string, PatchEntryChanges> BuildPatchLookup(Patch patch)
        {
            if (patch == null)
            {
                return null;
            }

            var lookup = new Dictionary<string, PatchEntryChanges>();

            foreach (var change in patch)
            {
                lookup[change.Path] = change;
            }

            return lookup;
        }

        private static (GitFileStatus? Status, bool IsStaged) MapFileStatus(FileStatus state)
        {
            if (state.HasFlag(FileStatus.NewInIndex)) return (GitFileStatus.Added, true);
            if (state.HasFlag(FileStatus.RenamedInIndex)) return (GitFileStatus.Renamed, true);
            if (state.HasFlag(FileStatus.DeletedFromIndex)) return (GitFileStatus.Deleted, true);
            if (state.HasFlag(FileStatus.TypeChangeInIndex)) return (GitFileStatus.Modified, true);
            if (state.HasFlag(FileStatus.ModifiedInIndex)) return (GitFileStatus.Modified, true);

            if (state.HasFlag(FileStatus.NewInWorkdir)) return (GitFileStatus.Added, false);
            if (state.HasFlag(FileStatus.RenamedInWorkdir)) return (GitFileStatus.Renamed, false);
            if (state.HasFlag(FileStatus.DeletedFromWorkdir)) return (GitFileStatus.Deleted, false);
            if (state.HasFlag(FileStatus.TypeChangeInWorkdir)) return (GitFileStatus.Modified, false);
            if (state.HasFlag(FileStatus.ModifiedInWorkdir)) return (GitFileStatus.Modified, false);

            return (null, false);
        }

        private static GitFileStatus? MapChangeKind(ChangeKind kind)
        {
            switch (kind)
            {
                case ChangeKind.Added:
                    return GitFileStatus.Added;
                case ChangeKind.Deleted:
                    return GitFileStatus.Deleted;
                case ChangeKind.Modified:
                    return GitFileStatus.Modified;
                case ChangeKind.Renamed:
                    return GitFileStatus.Renamed;
                case ChangeKind.Copied:
                    return GitFileStatus.Copied;
                case ChangeKind.TypeChanged:
                    return GitFileStatus.Modified;
                default:
                    return null;
            }
        }
    }
}
