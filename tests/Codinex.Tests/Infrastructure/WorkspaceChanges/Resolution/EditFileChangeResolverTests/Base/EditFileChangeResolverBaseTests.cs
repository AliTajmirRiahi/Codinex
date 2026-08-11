using Codinex.Core.Interfaces;
using Codinex.Core.Interfaces.Helper;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges.Resolution;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Resolution.EditFileChangeResolverTests.Base;

public abstract class EditFileChangeResolverBaseTests
{
    protected IWorkspaceFileService WorkspaceFileService = null!;
    protected ITextChangeMatcher TextChangeMatcher = null!;
    protected IStringHelper StringHelper = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
        TextChangeMatcher = new TextChangeMatcher();
        StringHelper = Substitute.For<IStringHelper>();
        StringHelper.Normalize(Arg.Any<string>()).Returns(call => call.Arg<string>());
    }

    protected virtual EditFileChangeResolver CreateSut()
    {
        return new EditFileChangeResolver(
            WorkspaceFileService,
            TextChangeMatcher,
            StringHelper);
    }
}
