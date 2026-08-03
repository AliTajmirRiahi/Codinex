using Codify.VisualStudio.Workspace.Formatters;

namespace Codify.Tests.VisualStudio.Workspace.FormatterTests.OpenDocumentsFormatterTest.Base;

public abstract class OpenDocumentsFormatterTestBase
{
    protected OpenDocumentsFormatter CreateSut()
    {
        return new OpenDocumentsFormatter();
    }
}