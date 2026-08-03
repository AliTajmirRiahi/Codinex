using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codify.Core.Models;

namespace Codify.Core.Interfaces
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