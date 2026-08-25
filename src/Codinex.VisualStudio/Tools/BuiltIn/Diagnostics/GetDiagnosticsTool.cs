using Codinex.Core.Models.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Context;
using Codinex.Core.Models.Tools;
using Codinex.Core.Tools;

namespace Codinex.VisualStudio.Tools.BuiltIn.Diagnostics;

/// <summary>
/// Gets compiler and analyzer diagnostics from the current workspace.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class GetDiagnosticsTool(
    IDiagnosticsProvider diagnosticsProvider)
    : IAiTool
{
    public string Name => "get_diagnostics";

    public string Description =>
        "Gets compiler and analyzer diagnostics from the current workspace.";

    public IReadOnlyList<string> Capabilities =>
    [
        "get diagnostics",
        "show diagnostics",
        "list diagnostics",
        "compiler diagnostics",
        "analyzer diagnostics",
        "errors and warnings",
        "show errors",
        "show warnings"
    ];

    public string StatusMessage => "Getting diagnostics...";

    public ToolVisibility Visibility { get; } = ToolVisibility.Model;

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>
        {
            ["scope"] = new ToolProperty(
                ToolPropertyType.String,
                "Diagnostics scope. Valid values: CurrentDocument, CurrentProject, Solution.")
            {
                Enum =
                [
                    nameof(DiagnosticsScope.CurrentDocument),
                    nameof(DiagnosticsScope.CurrentProject),
                    nameof(DiagnosticsScope.Solution)
                ]
            }
        },
        ["scope"]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scopeText = request.GetRequiredString("scope");

            if (!TryParseScope(scopeText, out var scope))
            {
                return ToolResult.Failed(
                    request.Id,
                    $"Unsupported diagnostics scope '{scopeText}'.");
            }

            var diagnostics =
                await diagnosticsProvider.GetDiagnosticsAsync(scope, cancellationToken);

            return ToolResult.Successful(
                request.Id,
                new
                {
                    Scope = scope.ToString(),
                    Count = diagnostics.Count,
                    Diagnostics = diagnostics.Select(diagnostic => new
                    {
                        diagnostic.Id,
                        diagnostic.ProjectName,
                        diagnostic.FilePath,
                        diagnostic.Line,
                        diagnostic.Column,
                        Severity = diagnostic.Severity.ToString(),
                        diagnostic.Message
                    })
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failed(
                request.Id,
                ex.Message);
        }
    }

    private static bool TryParseScope(
        string value,
        out DiagnosticsScope scope)
    {
        scope = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalizedValue = value.Trim();
        var separatorIndex = normalizedValue.LastIndexOfAny(['.', '#']);

        if (separatorIndex >= 0 && separatorIndex < normalizedValue.Length - 1)
        {
            normalizedValue = normalizedValue.Substring(separatorIndex + 1);
        }

        return Enum.TryParse(
            normalizedValue,
            true,
            out scope);
    }
}
