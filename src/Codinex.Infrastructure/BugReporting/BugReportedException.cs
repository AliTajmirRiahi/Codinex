using System;

namespace Codinex.Infrastructure.BugReporting
{
    /// <summary>
    /// Synthesized exception used to notify BugSnag when the user reports a bug without
    /// pasting an actual stack trace.
    /// </summary>
    public sealed class BugReportedException : Exception
    {
        public BugReportedException(string message) : base(message)
        {
        }
    }
}
