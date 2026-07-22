using Codify.Core.Workspace.Prompt;

namespace Codify.Infrastructure.Workspace.PromptPipeline;

/// <summary>
/// Creates prompt context items.
/// </summary>
public static class PromptContextItemFactory
{
    /// <summary>
    /// Creates a prompt context item.
    /// </summary>
    public static PromptContextItem Create(
        PromptContextKind kind,
        string title,
        string content)
    {
        return new PromptContextItem
        {
            Kind = kind,
            Title = title,
            Content = content
        };
    }
}