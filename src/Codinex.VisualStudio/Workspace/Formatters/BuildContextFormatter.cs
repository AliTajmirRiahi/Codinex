using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models.Context;

namespace Codinex.VisualStudio.Workspace.Formatters;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class BuildContextFormatter
    : IBuildContextFormatter
{
    public string Format(BuildContext context)
    {
        return
            $"""
             # Build Output

             {(string.IsNullOrWhiteSpace(context.Output) ? "(No build output available.)" : context.Output)}
             """;
    }
}