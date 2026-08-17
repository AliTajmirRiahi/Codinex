using System;
using System.Text;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces;
using Codinex.Core.Models;

namespace Codinex.VisualStudio.Workspace.Formatters;

/// <summary>
/// Formats Git context into a prompt-friendly markdown document.
/// </summary>
[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class GitContextFormatter : IGitContextFormatter
{
    public string Format(GitContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        var builder = new StringBuilder();

        builder.AppendLine("# Git Status");
        builder.AppendLine();

        builder.AppendLine($"Branch: {context.BranchName ?? "(Not a Git repository)"}");
        builder.AppendLine();

        if (context.Files == null || context.Files.Count == 0)
        {
            builder.AppendLine("(No pending changes.)");
            return builder.ToString();
        }

        foreach (var file in context.Files)
        {
            var scope = file.IsStaged ? "staged" : "unstaged";
            builder.AppendLine($"## [{file.Status}, {scope}] {file.Path} (+{file.LinesAdded} -{file.LinesDeleted})");
            builder.AppendLine();

            if (string.IsNullOrEmpty(file.Diff))
            {
                builder.AppendLine("(No diff available.)");
            }
            else
            {
                builder.AppendLine("```diff");
                builder.Append(file.Diff);

                if (!file.Diff.EndsWith("\n"))
                {
                    builder.AppendLine();
                }

                builder.AppendLine("```");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
