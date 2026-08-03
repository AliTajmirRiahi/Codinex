using System.Collections.Generic;

namespace Codinex.Core.Tools;

/// <summary>
/// Resolves AI tools by name.
/// </summary>
public interface IAiToolRegistry
{
    /// <summary>
    /// Resolves a tool by name.
    /// </summary>
    IAiTool Get(string toolName);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<IAiTool> GetAll();
}