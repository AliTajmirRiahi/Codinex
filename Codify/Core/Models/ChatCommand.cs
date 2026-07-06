namespace Codify.Core.Models
{
    public sealed class ChatCommand
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}