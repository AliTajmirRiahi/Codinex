using System;

namespace Codinex.VisualStudio.CommitMessages
{
    internal enum GitCommitPhase
    {
        Idle,
        Generating,
        ResultReady,
        Error
    }

    /// <summary>
    /// Tracks the state of an in-flight commit-message generation for the injected UI.
    /// </summary>
    internal sealed class GitCommitGenerationState
    {
        public GitCommitPhase Phase { get; private set; } = GitCommitPhase.Idle;

        public string ErrorMessage { get; private set; }

        public event Action<GitCommitPhase> PhaseChanged;

        public void SetGenerating()
        {
            Phase = GitCommitPhase.Generating;
            PhaseChanged?.Invoke(Phase);
        }

        public void SetResultReady()
        {
            Phase = GitCommitPhase.ResultReady;
            PhaseChanged?.Invoke(Phase);
        }

        public void SetError(string message)
        {
            ErrorMessage = message;
            Phase = GitCommitPhase.Error;
            PhaseChanged?.Invoke(Phase);
        }

        public void Reset()
        {
            ErrorMessage = null;
            Phase = GitCommitPhase.Idle;
            PhaseChanged?.Invoke(Phase);
        }
    }
}
