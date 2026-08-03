using Codinex.VisualStudio.Workspace.Formatters;

namespace Codinex.Tests.VisualStudio.Workspace.FormatterTests.OpenDocumentsFormatterTest.Base;

public abstract class OpenDocumentsFormatterTestBase
{
    protected OpenDocumentsFormatter CreateSut()
    {
        return new OpenDocumentsFormatter();
    }
}