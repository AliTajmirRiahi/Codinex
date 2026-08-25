using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Models.AI;

namespace Codinex.Core.Interfaces.AI;
public interface IProviderCapabilityChecker
{
    Task CheckAsync(
        AiProvider provider,
        AiModel model,
        CancellationToken cancellationToken = default);
}