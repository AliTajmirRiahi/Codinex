namespace Codinex.Storage.Models
{
    public class WorkspaceSettings
    {
        // System instruction applied to every chat in this solution, regardless of conversation group.
        public string SolutionInstruction { get; set; } = string.Empty;
    }
}
