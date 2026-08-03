using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Infrastructure.WorkspaceChanges;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeHandlerInvokerTests.Base;

public abstract class WorkspaceChangeHandlerInvokerBaseTests
{
    protected IWorkspaceChangeHandlerResolver WorkspaceChangeHandlerResolver = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceChangeHandlerResolver =
            Substitute.For<IWorkspaceChangeHandlerResolver>();
    }

    protected virtual WorkspaceChangeHandlerInvoker CreateSut()
    {
        return new WorkspaceChangeHandlerInvoker(
            WorkspaceChangeHandlerResolver);
    }
}