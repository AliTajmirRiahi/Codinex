using System;

namespace Codinex.Storage.Models.DTO;

public class ConversationGroupUpdateDto
{
    public Guid GroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
