using Newtonsoft.Json.Linq;
using System;

namespace Codify.Core.Conversation;

/// <summary>
/// Represents a tool execution request emitted by an AI provider.
/// </summary>
public sealed class ToolRequest
{
    /// <summary>
    /// Unique tool call identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Tool name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Provider supplied arguments.
    /// </summary>
    public JObject Arguments { get; set; } = new JObject();


    public string GetRequiredString(string name)
    {
        var value = Arguments.Value<string>(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required tool argument '{name}' was not provided.");
        }

        return value;
    }

    public string? GetString(string name)
    {
        return Arguments.Value<string>(name);
    }

    public bool GetBoolean(string name)
    {
        return Arguments.Value<bool>(name);
    }

    public int GetInt32(string name)
    {
        return Arguments.Value<int>(name);
    }

    public T GetObject<T>(string name)
    {
        return Arguments[name].ToObject<T>();
    }
}