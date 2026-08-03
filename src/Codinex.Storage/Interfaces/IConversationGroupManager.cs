using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codinex.Storage.Models;

namespace Codinex.Storage.Interfaces;

public interface IConversationGroupManager
{
    ConversationGroup CurrentGroup { get; }

    Task InitializeAsync();

    Task<IReadOnlyList<ConversationGroup>> GetAllGroupsAsync();

    Task<ConversationGroup?> GetGroupAsync(Guid groupId);

    Task<ConversationGroup> CreateGroupAsync(string name, string description);

    Task<ConversationGroup> CreateDefaultGroupAsync();

    Task SelectGroupAsync(Guid groupId);

    Task UpdateGroupAsync(ConversationGroup group);

    Task DeleteGroupAsync(Guid groupId);
}