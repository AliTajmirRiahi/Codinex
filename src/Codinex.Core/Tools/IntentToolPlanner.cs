using System;
using System.Collections.Generic;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;

namespace Codinex.Core.Tools;

/// <summary>
/// Static intent-to-tool planner used between the Preprocessor AI and the primary AI.
/// </summary>
[AutoDiRegister(Modules.Tool, RegistrationOrder.Foundation)]
public sealed class IntentToolPlanner : IIntentToolPlanner
{
    private static readonly IReadOnlyList<string> FallbackTools =
    [
        "build_project",
        "build_solution",
        "get_diagnostics",
        "get_file_elements",
        "read_element",
        "read_file",
        "forget_memory",
        "remember_memory",
        "get_open_documents",
        "get_projects",
        "find_references",
        "find_symbol",
        "search_project",
        "run_tests",
        "change_set_creator",
        "list_directory"
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ToolsByIntent =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ReadWorkspace"] = ["list_directory", "get_projects", "get_open_documents"],
            ["ReadProject"] = ["get_projects"],
            ["ReadProjects"] = ["get_projects"],
            ["ReadSolution"] = ["get_projects"],
            ["ReadFile"] = ["read_file"],
            ["ReadDirectory"] = ["list_directory"],
            ["ReadCodeElement"] = ["get_file_elements", "read_element"],
            ["SearchProject"] = ["search_project"],
            ["SearchFile"] = ["search_project"],
            ["SearchSymbol"] = ["find_symbol"],
            ["SearchText"] = ["search_project"],
            ["OpenDocuments"] = ["get_open_documents"],
            ["CurrentDocument"] = ["get_open_documents"],
            ["WorkspaceMemory"] = ["remember_memory", "forget_memory"],

            ["GenerateCode"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateFile"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateFolder"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateClass"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateInterface"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateRecord"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateStruct"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateEnum"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateMethod"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateProperty"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CreateTest"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "run_tests", "build_project", "build_solution"],
            ["Scaffold"] = ["get_file_elements", "read_element", "list_directory", "get_projects", "change_set_creator", "build_project", "build_solution"],

            ["EditCode"] = ["get_file_elements", "read_element", "read_file", "search_project", "get_file_elements", "change_set_creator", "build_project", "build_solution"],
            ["RefactorCode"] = ["get_file_elements", "read_element", "read_file", "search_project", "get_file_elements", "find_references", "change_set_creator", "build_project", "build_solution"],
            ["RenameSymbol"] = ["get_file_elements", "read_element", "find_symbol", "find_references", "read_file", "change_set_creator", "build_project", "build_solution"],
            ["ExtractMethod"] = ["get_file_elements", "read_element", "read_file", "get_file_elements", "change_set_creator", "build_project", "build_solution"],
            ["OptimizeCode"] = ["get_file_elements", "read_element", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["FixBug"] = ["get_file_elements", "read_element", "read_file", "search_project", "get_diagnostics", "change_set_creator", "build_project", "build_solution"],
            ["ImplementFeature"] = ["get_file_elements", "read_element", "list_directory", "get_projects", "read_file", "search_project", "change_set_creator", "build_project", "build_solution"],
            ["CompleteCode"] = ["get_file_elements", "read_element", "read_file", "get_file_elements", "change_set_creator", "build_project", "build_solution"],
            ["ApplyChangeSet"] = ["get_file_elements", "read_element", "change_set_creator", "build_project", "build_solution"],

            ["BuildProject"] = ["build_project"],
            ["BuildSolution"] = ["build_solution"],
            ["CleanSolution"] = [],
            ["RestorePackages"] = [],
            ["RunProject"] = [],

            ["RunTests"] = ["run_tests"],
            ["RunTest"] = ["run_tests"],
            ["GenerateTests"] = ["read_file", "search_project", "change_set_creator", "run_tests"],
            ["FixTests"] = ["run_tests", "get_diagnostics", "read_file", "search_project", "change_set_creator"],

            ["GetDiagnostics"] = ["get_diagnostics"],
            ["FixDiagnostics"] = ["get_diagnostics", "read_file", "search_project", "change_set_creator"],
            ["AnalyzeBuildFailure"] = ["build_solution", "get_diagnostics", "read_file", "search_project"],

            ["ExplainCode"] = ["read_file", "get_file_elements", "read_element"],
            ["ReviewCode"] = ["read_file", "search_project", "get_file_elements"],
            ["AnalyzeCode"] = ["read_file", "search_project", "get_file_elements", "read_element"],
            ["AnalyzeArchitecture"] = ["list_directory", "get_projects", "search_project", "read_file"],
            ["PerformanceAnalysis"] = ["read_file", "search_project", "get_file_elements"],
            ["SecurityAnalysis"] = ["read_file", "search_project", "get_file_elements"],
            ["FindBug"] = ["read_file", "search_project", "get_diagnostics"],
            ["FindDeadCode"] = ["search_project", "find_references", "find_symbol"],
            ["FindUsages"] = ["find_references", "find_symbol", "search_project"],
            ["FindReferences"] = ["find_references", "find_symbol", "search_project"],

            ["FindFile"] = ["search_project"],
            ["FindType"] = ["find_symbol"],
            ["FindMethod"] = ["find_symbol", "get_file_elements"],
            ["FindClass"] = ["find_symbol"],
            ["FindInterface"] = ["find_symbol"],
            ["GoToDefinition"] = ["find_symbol", "read_element"],
            ["GoToImplementation"] = ["find_symbol", "find_references"],

            ["GitStatus"] = [],
            ["GitDiff"] = [],
            ["GitLog"] = [],
            ["GitBranches"] = [],
            ["GitCommit"] = [],
            ["GitStage"] = [],
            ["GitCheckout"] = [],
            ["GitMerge"] = [],

            ["Remember"] = ["remember_memory"],
            ["Forget"] = ["forget_memory"],
            ["Recall"] = [],

            ["CreatePlan"] = [],
            ["BreakIntoSteps"] = [],
            ["EstimateComplexity"] = [],

            ["WebSearch"] = [],
            ["OpenUrl"] = [],
            ["DownloadResource"] = [],

            ["Unknown"] = FallbackTools
        };

    public IReadOnlyList<string> PlanTools(IReadOnlyList<string> intents)
    {
        if (intents == null || intents.Count == 0)
        {
            return FallbackTools;
        }

        var plannedTools = new List<string>();
        var plannedToolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var intent in intents)
        {
            if (string.IsNullOrWhiteSpace(intent))
            {
                continue;
            }

            if (!ToolsByIntent.TryGetValue(intent.Trim(), out var tools))
            {
                tools = FallbackTools;
            }

            foreach (var tool in tools)
            {
                if (plannedToolNames.Add(tool))
                {
                    plannedTools.Add(tool);
                }
            }
        }

        return plannedTools;
    }
}
