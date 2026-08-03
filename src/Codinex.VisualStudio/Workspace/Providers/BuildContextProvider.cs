using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Workspace.Providers;

/// <summary>
/// Provides build context.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class BuildContextProvider(
    IVsOutputWindowService outputWindowService)
    : IBuildContextProvider
{
    public async Task<BuildContext> GetContextAsync(CancellationToken cancellationToken)
    {
        var output =
            await outputWindowService.ReadOutputAsync("Build", cancellationToken);

        return new BuildContext
        {
            Output = string.IsNullOrWhiteSpace(output) ? "" : output
        };
    }
}