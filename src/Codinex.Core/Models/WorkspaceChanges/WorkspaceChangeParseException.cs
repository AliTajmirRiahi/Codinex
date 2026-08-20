using System;

namespace Codinex.Core.Models.WorkspaceChanges;

/// <summary>
/// Raised when an AI-provided workspace change payload cannot be interpreted
/// (e.g. an unknown or missing 'kind' discriminator). Callers should catch this
/// and report it back to the AI model instead of letting it crash the turn.
/// </summary>
public sealed class WorkspaceChangeParseException : Exception
{
    public WorkspaceChangeParseException(string message)
        : base(message)
    {
    }

    public WorkspaceChangeParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
