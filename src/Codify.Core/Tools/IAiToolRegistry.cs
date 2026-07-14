namespace Codify.Core.Tools;

/// <summary>
/// Resolves AI tools by name.
/// </summary>
public interface IAiToolRegistry
{
    /// <summary>
    /// Resolves a tool by name.
    /// </summary>
    IAiTool Get(string toolName);
}