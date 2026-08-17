using System;

namespace Codinex.Core.Chat
{
    /// <summary>
    /// Thrown when a commit message is requested but there are no pending Git changes.
    /// </summary>
    public sealed class NoGitChangesException : Exception
    {
        public NoGitChangesException()
            : base("There are no pending Git changes.")
        {
        }
    }
}
