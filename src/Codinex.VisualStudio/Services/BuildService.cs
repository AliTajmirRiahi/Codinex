using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Core.Models;
using Codify.VisualStudio.Interfaces;
using Codify.VisualStudio.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codify.VisualStudio.Services;

/// <summary>
/// Provides Visual Studio solution build operations.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Foundation)]
public sealed class BuildService(IVisualStudioServices visualStudioServices, IBuildContextProvider buildContextProvider, IDiagnosticsProvider diagnosticsProvider) : IBuildService
{
    public async Task<BuildResult> BuildSolutionAsync(
        CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var buildManager = await visualStudioServices.GetSolutionBuildManagerAsync();

        if (buildManager is not IVsSolutionBuildManager2 buildManager2)
        {
            return new BuildResult()
            {
                Success = false,
                ErrorCount = 0,
                Summary = $"Unable to build solution.",
                Output = "",
            };
        }

        var hr = buildManager.StartSimpleUpdateSolutionConfiguration(
            (uint)(
                VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD |
                VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE),
            (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_OUTOFDATE_QUERY_YES,
            0);

        ErrorHandler.ThrowOnFailure(hr);

        ErrorHandler.ThrowOnFailure(
            buildManager2.QueryBuildManagerBusy(out var busy));

        var success = true;

        while (busy != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(200, cancellationToken);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ErrorHandler.ThrowOnFailure(
                buildManager2.QueryBuildManagerBusy(out busy));
        }

        var outputContext = await buildContextProvider.GetContextAsync(cancellationToken);

        var diagnostics =
            await diagnosticsProvider.GetDiagnosticsAsync(DiagnosticsScope.Solution, cancellationToken);

        var errors = diagnostics.Where(p => p.Severity == DiagnosticSeverity.Error);

        var diagnosticItems = errors as DiagnosticItem[] ?? errors.ToArray();

        success = !diagnosticItems.Any();

        var errorCount = diagnosticItems.Count();

        return new BuildResult
        {
            Success = success,
            ErrorCount = errorCount,
            Errors = diagnosticItems,
            Summary = success
                ? "Build succeeded."
                : $"Build failed with {errorCount} error(s).",
            Output = outputContext.Output,
        };

    }
}