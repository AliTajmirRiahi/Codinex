using System.Collections.Generic;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.Context
{
    /// <summary>
    /// Formats diagnostics into prompt text.
    /// </summary>
    public interface IDiagnosticsFormatter
    {
        string Format(IReadOnlyList<DiagnosticItem> diagnostics);
    }
}