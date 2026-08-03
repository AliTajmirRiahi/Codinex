using System.Collections.Generic;
using Codify.Core.Models;

namespace Codify.VisualStudio.Models;

/// <summary>
/// Represents the result of a solution build.
/// </summary>
public sealed class BuildResult
{
    public bool Success { get; set; }

    public int ErrorCount { get; set; }

    public int WarningCount { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Output { get; set; } = string.Empty;

    public IReadOnlyList<DiagnosticItem> Errors { get; set; } = [];
}