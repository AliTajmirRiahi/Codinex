using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.Context;

namespace Codinex.Core.Interfaces.Context
{
    /// <summary>
    /// Provides diagnostics from the current workspace.
    /// </summary>
    public interface IDiagnosticsProvider
    {
        Task<IReadOnlyList<DiagnosticItem>> GetDiagnosticsAsync(
            DiagnosticsScope scope,
            CancellationToken cancellationToken);
    }
}