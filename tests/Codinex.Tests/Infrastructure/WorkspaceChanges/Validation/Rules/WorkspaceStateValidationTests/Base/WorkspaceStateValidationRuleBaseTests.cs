using Codinex.Core.Interfaces.Workspace;
using Codinex.Infrastructure.WorkspaceChanges.Validation.Rules;
using NSubstitute;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.WorkspaceChanges.Validation.Rules.WorkspaceStateValidationTests.Base;

public abstract class WorkspaceStateValidationRuleBaseTests
{
    protected IWorkspaceContext WorkspaceContext = null!;

    protected WorkspaceStateValidationRule Sut = null!;

    [SetUp]
    public virtual void SetUp()
    {
        WorkspaceContext = Substitute.For<IWorkspaceContext>();

        WorkspaceContext.SolutionDirectory
            .Returns(@"C:\Workspace\TestSolution");

        Sut = CreateSut();
    }

    protected virtual WorkspaceStateValidationRule CreateSut()
    {
        return new WorkspaceStateValidationRule(WorkspaceContext);
    }
}