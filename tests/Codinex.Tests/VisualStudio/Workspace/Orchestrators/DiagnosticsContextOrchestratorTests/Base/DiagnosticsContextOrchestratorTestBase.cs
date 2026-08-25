using Codinex.Core.Interfaces.Context;
using Codinex.VisualStudio.Workspace.Orchestrators;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.VisualStudio.Workspace.Orchestrators.DiagnosticsContextOrchestratorTests.Base;

public abstract class DiagnosticsContextOrchestratorTestBase
{
    protected IDiagnosticsProvider DiagnosticsProvider = null!;
    protected IDiagnosticsFormatter DiagnosticsFormatter = null!;

    [SetUp]
    public virtual void SetUp()
    {
        DiagnosticsProvider = Substitute.For<IDiagnosticsProvider>();
        DiagnosticsFormatter = Substitute.For<IDiagnosticsFormatter>();
    }

    protected DiagnosticsContextOrchestrator CreateSut()
    {
        return new DiagnosticsContextOrchestrator(
            DiagnosticsProvider,
            DiagnosticsFormatter);
    }
}