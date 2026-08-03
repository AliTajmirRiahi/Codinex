using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models;

namespace Codinex.Core.Interfaces;
public interface IProviderCapabilityChecker
{
    Task CheckAsync(
        AiProvider provider,
        AiModel model,
        CancellationToken cancellationToken = default);
}