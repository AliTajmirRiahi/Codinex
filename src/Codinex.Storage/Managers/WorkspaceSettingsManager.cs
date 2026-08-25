using System;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Core.Interfaces.Workspace;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Models;
using Codinex.Storage.Services;
using Newtonsoft.Json;

namespace Codinex.Storage.Managers
{
    /// <summary>
    /// Manages settings scoped to the currently open solution (workspace),
    /// such as the solution-wide instruction prompt applied to every chat.
    /// </summary>
    [AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation)]
    public sealed class WorkspaceSettingsManager(
        IStorageService storage,
        IWorkspaceContext workspaceContext) : IWorkspaceSettingsManager
    {
        private WorkspaceSettings _currentSettings = new();

        public WorkspaceSettings Settings => _currentSettings;

        /// <summary>
        /// Loads workspace settings from the current solution's storage folder, or creates defaults if not found.
        /// </summary>
        public async Task InitializeAsync()
        {
            var path = StoragePaths.GetWorkspaceSettingsPath(WorkspaceName);

            if (!await storage.ExistsAsync(path))
            {
                await SaveAsync(_currentSettings);
                return;
            }

            try
            {
                var loaded = await storage.LoadAsync<WorkspaceSettings>(path);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                    return;
                }
            }
            catch (JsonException)
            {
                // If settings.json is empty or invalid, restore the default settings.
            }

            await SaveAsync(_currentSettings);
        }

        /// <summary>
        /// Updates and persists settings for the current solution.
        /// </summary>
        public async Task SaveAsync(WorkspaceSettings newSettings)
        {
            if (newSettings == null)
                throw new ArgumentNullException(nameof(newSettings));

            _currentSettings = newSettings;

            var path = StoragePaths.GetWorkspaceSettingsPath(WorkspaceName);

            await storage.SaveAsync(path, _currentSettings);
        }

        private string WorkspaceName =>
            workspaceContext.IsSolutionOpen && !string.IsNullOrWhiteSpace(workspaceContext.SolutionName)
                ? workspaceContext.SolutionName
                : "DefaultSolution";
    }
}
