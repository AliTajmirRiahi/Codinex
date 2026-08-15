namespace Codinex.Storage.Models.DTO;

public class SolutionInstructionDto
{
    public string SolutionInstruction { get; set; } = string.Empty;

    public string ExcludeDirectories { get; set; } = string.Empty;

    public string ExcludeFiles { get; set; } = string.Empty;

    public string IgnoredExtensions { get; set; } = string.Empty;

    public string IgnoredFileSuffixes { get; set; } = string.Empty;
}
