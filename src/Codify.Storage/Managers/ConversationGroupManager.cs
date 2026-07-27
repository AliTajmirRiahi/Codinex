using Codify.Core.DependencyInjection.Attributes;
using Codify.Core.DependencyInjection.Models;
using Codify.Core.Interfaces;
using Codify.Storage.Interfaces;
using Codify.Storage.Models;
using Codify.Storage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Codify.Storage.Managers;

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
        var group = new ConversationGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
            CurrentGroup = group;
            return group;
        }

        group = await CreateGroupAsync(
            "Default Project",
            "Default Conversation Project");

        group.IsDefault = true;

        await UpdateGroupAsync(group);

        return group;
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

        await storageService.SaveAsync(
            StoragePaths.GetGroupJsonPath(
                WorkspaceName,
                current.GetId()),
            current);
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        var group = _groups.FirstOrDefault(x => x.Id == groupId);

        if (group == null)
        {
            return;
        }

        await storageService.DeleteAsync(
            StoragePaths.GetGroupPath(
                WorkspaceName,
                group.GetId()));

        _groups.Remove(group);
    }

    private string WorkspaceName =>
        workspaceContext.IsSolutionOpen
            ? workspaceContext.SolutionName
            : "DefaultSolution";

    private string GroupsPath =>
        StoragePaths.GetGroupsPath(WorkspaceName);
}