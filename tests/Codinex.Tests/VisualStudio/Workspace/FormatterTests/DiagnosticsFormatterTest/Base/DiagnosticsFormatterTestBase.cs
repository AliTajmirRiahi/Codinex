using Codinex.VisualStudio.Workspace.Formatters;

namespace Codinex.Tests.VisualStudio.Workspace.FormatterTests.DiagnosticsFormatterTest.Base;

public abstract class DiagnosticsFormatterTestBase
{
    protected DiagnosticsFormatter CreateSut()
    {
        return new DiagnosticsFormatter();
    }
}