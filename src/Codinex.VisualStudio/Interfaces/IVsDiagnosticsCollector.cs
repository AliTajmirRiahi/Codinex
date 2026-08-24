using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Codinex.VisualStudio.Interfaces
{
    /// <summary>
    /// Collects Visual Studio environment diagnostics (output pane log, IDE version/edition)
    /// for attaching to bug reports, both user-submitted and auto-filed.
    /// </summary>
    public interface IVsDiagnosticsCollector
    {
        Task<string> CollectOutputLogAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, string>> CollectVsInfoAsync();
    }
}
