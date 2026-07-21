using Codify.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.Core.Interfaces;
public interface IProviderCapabilityChecker
{
    Task CheckAsync(
        AiProvider provider,
        AiModel model,
        CancellationToken cancellationToken = default);
}