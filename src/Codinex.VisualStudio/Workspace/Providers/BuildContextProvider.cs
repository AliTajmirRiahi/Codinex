using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models.Context;
using Codinex.VisualStudio.Interfaces;

namespace Codinex.VisualStudio.Workspace.Providers;

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