using System;
using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Models;
using Codinex.Storage.Services;
using Newtonsoft.Json;

namespace Codinex.Storage.Managers
{
    [AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation)]
    public class SettingsManager(IStorageService storage)
    {
        private CodinexSettings _currentSettings = new();

        public CodinexSettings Settings => _currentSettings;

        /// <summary>
        /// Loads global settings from %LocalAppData%\Codinex\settings.json, or creates defaults if not found.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (!await storage.ExistsAsync(StoragePaths.Settings))
            {
                await SaveSettingsAsync(_currentSettings);
                return;
            }

            try
            {
                var loaded = await storage.LoadAsync<CodinexSettings>(StoragePaths.Settings);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                    // Load CommitMessageSystemPrompt from settings if available
                    if (!string.IsNullOrEmpty(loaded.CommitMessageSystemPrompt))
                    {
                        _currentSettings.CommitMessageSystemPrompt = "You write Git commit messages from a unified diff.\r\n" + loaded.CommitMessageSystemPrompt;
                    }
                    return;
                }
            }
            catch (JsonException)
            {
                // If settings.json is empty or invalid, restore the default settings.
            }

            await SaveSettingsAsync(_currentSettings);
        }

        /// <summary>
        /// Updates and persists global settings to %LocalAppData%\Codinex\settings.json.
        /// </summary>
        public async Task SaveSettingsAsync(CodinexSettings newSettings)
        {
            _currentSettings = newSettings ?? throw new ArgumentNullException(nameof(newSettings));

            await storage.SaveAsync(StoragePaths.Settings, _currentSettings);
        }

        /// <summary>
        /// Gets the current commit message system prompt from settings.
        /// </summary>
        public string GetCommitMessageSystemPrompt()
        {
            return _currentSettings.CommitMessageSystemPrompt;
        }
    }
}