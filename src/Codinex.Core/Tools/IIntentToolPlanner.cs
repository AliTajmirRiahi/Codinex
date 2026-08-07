using System.Collections.Generic;

namespace Codinex.Core.Tools;

/// <summary>
/// Maps preprocessor intents to the model tools that should be exposed to the primary AI.
/// </summary>
public interface IIntentToolPlanner
{
    /// <summary>
    /// Returns the tool names that are allowed for the specified intents.
    /// </summary>
    IReadOnlyList<string> PlanTools(IReadOnlyList<string> intents);
}
