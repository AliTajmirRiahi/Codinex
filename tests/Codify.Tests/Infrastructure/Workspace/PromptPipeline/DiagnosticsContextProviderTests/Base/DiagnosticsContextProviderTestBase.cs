using Codify.Core.Interfaces;
using Codify.VisualStudio.Workspace.Providers;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.Workspace.PromptPipeline.DiagnosticsContextProviderTests.Base;

public abstract class DiagnosticsContextProviderTestBase
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