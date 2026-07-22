using System.Collections.Generic;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    /// <summary>
    /// Formats diagnostics into prompt text.
    /// </summary>
    public interface IDiagnosticsFormatter
    {
        string Format(IReadOnlyList<DiagnosticItem> diagnostics);
    }
}