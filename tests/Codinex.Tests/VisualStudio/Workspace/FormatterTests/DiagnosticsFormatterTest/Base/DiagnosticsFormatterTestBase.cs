using Codify.VisualStudio.Workspace.Formatters;

namespace Codify.Tests.VisualStudio.Workspace.FormatterTests.DiagnosticsFormatterTest.Base;

public abstract class DiagnosticsFormatterTestBase
{
    protected DiagnosticsFormatter CreateSut()
    {
        return new DiagnosticsFormatter();
    }
}