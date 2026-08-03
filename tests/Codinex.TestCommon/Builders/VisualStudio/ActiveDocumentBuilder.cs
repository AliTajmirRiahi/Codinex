using EnvDTE;
using NSubstitute;

namespace Codify.TestCommon.Builders.VisualStudio;

#pragma warning disable VSTHRD010
public sealed class ActiveDocumentBuilder
{
    private string _filePath = string.Empty;

    public ActiveDocumentBuilder WithFile(string path)
    {
        _filePath = path;
        return this;
    }

    public Document Build()
    {
        var document = Substitute.For<Document>();

        document.FullName.Returns(_filePath);

        return document;
    }
}
#pragma warning restore VSTHRD010