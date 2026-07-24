using Codify.Core.Interfaces;
using Codify.Core.Models;

namespace Codify.VisualStudio.Workspace.Formatters;

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