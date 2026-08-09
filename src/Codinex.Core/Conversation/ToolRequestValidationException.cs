using System;

namespace Codinex.Core.Conversation;

/// <summary>
/// Represents a tool request validation error that should be returned to the model.
/// </summary>
public sealed class ToolRequestValidationException(
    string toolName,
    string argumentName) : Exception(CreateMessage(toolName, argumentName))
{
    public string ToolName { get; } = toolName;

    public string ArgumentName { get; } = argumentName;

    private static string CreateMessage(
        string toolName,
        string argumentName)
    {
        return $"The tool '{toolName}' requires the '{argumentName}' argument. Please call the tool again with this argument provided.";
    }
}
