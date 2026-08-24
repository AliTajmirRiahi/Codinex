using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    /// <summary>
    /// Submits user-reported bugs to BugSnag, attaching system info, the Codinex output
    /// pane log, and the reporting chat's last recorded AI request prompt as metadata.
    /// </summary>
    public interface IBugReportService
    {
        Task<BugReportResult> SubmitAsync(
            string chatId,
            string description,
            string outputLog,
            IReadOnlyDictionary<string, string> vsInfo,
            CancellationToken cancellationToken = default);
    }
}
