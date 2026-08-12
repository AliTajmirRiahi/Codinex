using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.WorkspaceChanges;
using Codinex.Core.Models.WorkspaceChanges;

namespace Codinex.Infrastructure.WorkspaceChanges.Resolution;

[AutoDiRegister(Modules.MissionEngine, RegistrationOrder.Foundation)]
public sealed class EditFileChangeResolutionContext : IEditFileChangeResolutionContext
{
    public ChangeValidationResult Current { get; set; }
}
