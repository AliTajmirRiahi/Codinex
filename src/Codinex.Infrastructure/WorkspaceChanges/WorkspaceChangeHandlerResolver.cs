using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces.WorkspaceChanges;
using Codify.Core.Models.WorkspaceChanges;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Codify.Infrastructure.WorkspaceChanges
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
