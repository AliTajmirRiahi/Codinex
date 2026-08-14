using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codinex.Core.Conversation;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Models.Tools;
using Codinex.Core.Models.Tools.AskUserQuestion;
using Codinex.Core.Tools;

namespace Codinex.VisualStudio.Tools.BuiltIn.Clarification;

[AutoDiRegister(Modules.Tool, RegistrationOrder.Platform)]
public sealed class AskUserQuestionTool(IClarificationSessionService clarificationSessionService) : IAiTool
{
    public string Name => "ask_user_question";

    public string Description =>
        "Ask the user one or more clarifying questions when the task is ambiguous, underspecified, " +
        "or has multiple reasonable interpretations - before doing risky or large work on a guess. " +
        "Do not use this for trivial or already-clear requests.\n\n" +
        "Give each question at least 3 concrete, mutually exclusive answer options with a short header " +
        "and a description, and mark exactly one option as recommended where a best choice exists. " +
        "The user may also type a free-form answer instead of picking an option, so treat the options " +
        "as suggestions, not the only valid outcomes. The tool returns the user's answer to every " +
        "question in the order they were asked.";

    public IReadOnlyList<string> Capabilities =>
    [
        "ask user a question",
        "clarify requirements",
        "clarify task",
        "resolve ambiguity",
        "request user input"
    ];

    public ToolVisibility Visibility => ToolVisibility.Model;

    public string StatusMessage => "Waiting for your answer...";

    public ToolDefinition Definition => new(
        new Dictionary<string, ToolProperty>
        {
            ["questions"] = new ToolProperty(ToolPropertyType.Array, "The questions to ask the user, in order.")
            {
                Items = new ToolProperty(ToolPropertyType.Object, "A single clarifying question.")
                {
                    Properties = new Dictionary<string, ToolProperty>
                    {
                        ["header"] = new ToolProperty(ToolPropertyType.String, "A short label for this question (a few words)."),
                        ["question"] = new ToolProperty(ToolPropertyType.String, "The full question text shown to the user."),
                        ["options"] = new ToolProperty(ToolPropertyType.Array, "At least 3 suggested answers.")
                        {
                            Items = new ToolProperty(ToolPropertyType.Object, "A single suggested answer.")
                            {
                                Properties = new Dictionary<string, ToolProperty>
                                {
                                    ["label"] = new ToolProperty(ToolPropertyType.String, "Short title for this option."),
                                    ["description"] = new ToolProperty(ToolPropertyType.String, "Explanation of what choosing this option means."),
                                    ["recommended"] = new ToolProperty(ToolPropertyType.Boolean, "Whether this is the recommended option.")
                                },
                                Required = ["label", "description"]
                            }
                        }
                    },
                    Required = ["header", "question", "options"]
                }
            }
        },
        ["questions"]);

    public async Task<ToolResult> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken)
    {
        var questions = request.GetObject<List<ClarificationQuestion>>("questions");

        if (questions is not { Count: > 0 })
        {
            return ToolResult.Failed(request.Id, "At least one question is required.");
        }

        var answers = await clarificationSessionService.AskAsync(request.Id, questions, cancellationToken);

        if (answers == null)
        {
            return ToolResult.Failed(request.Id, "The user closed or cancelled the question without answering.");
        }

        return ToolResult.Successful(request.Id, new { answers });
    }
}
