using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Models;
using Codinex.Storage.Services;

namespace Codinex.Storage.Managers;

[AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation)]
public sealed class ConversationGroupManager(
    IStorageService storageService,
    IWorkspaceContext workspaceContext)
    : IConversationGroupManager
{
    private readonly List<ConversationGroup> _groups = [];

    public ConversationGroup CurrentGroup { get; private set; }

    public async Task InitializeAsync()
    {
        _groups.Clear();

        var directories = await storageService.GetDirectoriesAsync(GroupsPath);

        foreach (var directory in directories)
        {
            var groupId = System.IO.Path.GetFileName(directory);

            var groupJsonPath = StoragePaths.GetGroupJsonPath(
                WorkspaceName,
                groupId);

            if (!await storageService.ExistsAsync(groupJsonPath))
            {
                continue;
            }

            var group = await storageService.LoadAsync<ConversationGroup>(groupJsonPath);

            if (group != null)
            {
                _groups.Add(group);
            }
        }

        CurrentGroup = _groups.FirstOrDefault(g => g.IsSelected)
            ?? _groups.FirstOrDefault(g => g.IsDefault)
            ?? _groups.FirstOrDefault();
    }

    public Task<IReadOnlyList<ConversationGroup>> GetAllGroupsAsync()
    {
        return Task.FromResult<IReadOnlyList<ConversationGroup>>(
            _groups.AsReadOnly());
    }

    public Task<ConversationGroup> GetGroupAsync(Guid groupId)
    {
        var group = _groups.FirstOrDefault(x => x.Id == groupId);

        return Task.FromResult(group);
    }

    public async Task<ConversationGroup> CreateGroupAsync(
        string name,
        string description)
    {
        await DeselectAllGroupsAsync();

        var group = new ConversationGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsSelected = true
        };

        var groupPath = StoragePaths.GetGroupPath(
            WorkspaceName,
            group.GetId());

        await storageService.CreateDirectoryAsync(groupPath);

        await storageService.SaveAsync(
            StoragePaths.GetGroupJsonPath(
                WorkspaceName,
                group.GetId()),
            group);

        _groups.Add(group);

        CurrentGroup = group;

        return group;
    }

    public async Task<ConversationGroup> CreateDefaultGroupAsync()
    {
        var group = _groups.FirstOrDefault(g => g.IsDefault);

        if (group != null)
        {
            if (CurrentGroup == null)
            {
                await SelectGroupAsync(group.Id);
            }

            return group;
        }

        group = await CreateGroupAsync(
            "Default Project",
            "Default Conversation Project");

        group.IsDefault = true;

        await UpdateGroupAsync(group);

        return group;
    }

    public async Task SelectGroupAsync(Guid groupId)
    {
        var group = _groups.FirstOrDefault(x => x.Id == groupId);

        if (group == null)
        {
            throw new InvalidOperationException(
                $"Conversation group '{groupId}' was not found.");
        }

        foreach (var item in _groups)
        {
            var shouldBeSelected = item.Id == groupId;

            if (item.IsSelected == shouldBeSelected)
            {
                continue;
            }

            item.IsSelected = shouldBeSelected;
            await SaveGroupAsync(item);
        }

        CurrentGroup = group;
    }

    public async Task UpdateGroupAsync(ConversationGroup group)
    {
        var current = _groups.FirstOrDefault(x => x.Id == group.Id);

        if (current == null)
        {
            throw new InvalidOperationException(
                $"Conversation group '{group.Id}' was not found.");
        }

        current.Name = group.Name;
        current.Description = group.Description;
        current.UpdatedAt = DateTime.UtcNow;

        await SaveGroupAsync(current);
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        var group = _groups.FirstOrDefault(x => x.Id == groupId);

        if (group == null)
        {
            return;
        }

        if (group.IsDefault)
        {
            throw new InvalidOperationException(
                "The default conversation group cannot be deleted.");
        }

        var isCurrentGroup = CurrentGroup != null && CurrentGroup.Id == groupId;

        var groupPath = StoragePaths.GetGroupPath(
            WorkspaceName,
            group.GetId());

        await DeleteGroupPromptsAsync(groupPath);

        await storageService.DeleteDirectoryAsync(groupPath);

        _groups.Remove(group);

        if (!_groups.Any())
        {
            await CreateDefaultGroupAsync();
            return;
        }

        if (isCurrentGroup)
        {
            var nextGroup = _groups.FirstOrDefault(g => g.IsDefault)
                ?? _groups.FirstOrDefault();

            if (nextGroup != null)
            {
                await SelectGroupAsync(nextGroup.Id);
            }
        }
    }

    /// <summary>
    /// Cascade-deletes the prompt-recording folders (chat_&lt;chatId&gt;) belonging to every
    /// chat within a group, before the group's own directory is removed.
    /// </summary>
    private async Task DeleteGroupPromptsAsync(string groupPath)
    {
        var files = await storageService.GetFilesAsync(groupPath);

        foreach (var file in files)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(file);

            if (!fileName.StartsWith("chat-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var chatId = fileName.Substring("chat-".Length);
            var promptsPath = StoragePaths.GetChatPromptsPath(chatId);

            if (await storageService.ExistsAsync(promptsPath))
            {
                await storageService.DeleteDirectoryAsync(promptsPath);
            }
        }
    }

    private async Task DeselectAllGroupsAsync()
    {
        foreach (var group in _groups.Where(group => group.IsSelected))
        {
            group.IsSelected = false;
            await SaveGroupAsync(group);
        }
    }

    private Task SaveGroupAsync(ConversationGroup group)
    {
        return storageService.SaveAsync(
            StoragePaths.GetGroupJsonPath(
                WorkspaceName,
                group.GetId()),
            group);
    }

    private string WorkspaceName =>
        workspaceContext.IsSolutionOpen
            ? workspaceContext.SolutionName
            : "DefaultSolution";

    private string GroupsPath =>
        StoragePaths.GetGroupsPath(WorkspaceName);
}