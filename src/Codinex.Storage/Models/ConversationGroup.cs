using System;

namespace Codinex.Storage.Models;

public sealed class ConversationGroup
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDefault { get; set; }

    public string GetId()
    {
        return Id.ToString();
    }
}