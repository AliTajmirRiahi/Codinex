using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Infrastructure.WorkspaceChanges;
using NSubstitute;
using NUnit.Framework;

namespace Codify.Tests.Infrastructure.WorkspaceChanges.WorkspaceChangeApplierTests.Base;

public abstract class WorkspaceChangeApplierBaseTests
{
    protected IWorkspaceChangeHandlerInvoker WorkspaceChangeHandlerInvoker = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceChangeHandlerInvoker =
            Substitute.For<IWorkspaceChangeHandlerInvoker>();
    }

    protected virtual WorkspaceChangeApplier CreateSut()
    {
        return new WorkspaceChangeApplier(
            WorkspaceChangeHandlerInvoker);
    }
}