namespace Codify.Core.Workspace.Prompt
{
    /// <summary>
    /// Identifies the origin of a prompt context item.
    /// </summary>
    public enum PromptContextKind
    {
        Unknown = 0,

        Conversation = 1,

        Workspace = 2,

        CurrentDocument = 3,

        Memory = 4,

        Project = 5,

        Git = 6,

        Diagnostics = 7,

        Tool = 8,
        
        OpenDocuments = 9,

        Build = 10,
    }
}