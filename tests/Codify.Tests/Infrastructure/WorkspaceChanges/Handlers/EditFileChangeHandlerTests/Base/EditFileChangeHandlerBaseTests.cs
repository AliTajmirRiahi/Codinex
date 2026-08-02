using Codify.Core.Interfaces;
using Codify.Core.Interfaces.Helper;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges.Handlers;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.Handlers.EditFileChangeHandlerTests.Base;

public abstract class EditFileChangeHandlerBaseTests
{
    protected IWorkspaceFileService WorkspaceFileService = null!;
    protected ITextChangeMatcher TextChangeMatcher = null!;
    protected IWorkspaceChangeErrorFactory WorkspaceChangeErrorFactory = null!;
    protected IStringHelper StringHelper = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceFileService = Substitute.For<IWorkspaceFileService>();
        TextChangeMatcher = Substitute.For<ITextChangeMatcher>();
        WorkspaceChangeErrorFactory = Substitute.For<IWorkspaceChangeErrorFactory>();
        StringHelper = Substitute.For<IStringHelper>();
        StringHelper.Normalize(Arg.Any<string>()).Returns(call => call.Arg<string>());
    }

    protected virtual EditFileChangeHandler CreateSut()
    {
        return new EditFileChangeHandler(
            WorkspaceFileService,
            TextChangeMatcher,
            WorkspaceChangeErrorFactory,
            StringHelper);
    }
}