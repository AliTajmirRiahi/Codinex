using Microsoft.Extensions.DependencyInjection;
using System;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges
{
    [AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Features)]
    public sealed class WorkspaceChangeHandlerResolver(
        IServiceProvider serviceProvider)
        : IWorkspaceChangeHandlerResolver
    {
        public IWorkspaceChangeHandler<TChange> Resolve<TChange>()
            where TChange : WorkspaceChange
        {
            return serviceProvider
                .GetRequiredService<IWorkspaceChangeHandler<TChange>>();
        }
    }
}
