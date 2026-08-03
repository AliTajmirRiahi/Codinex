using System.Threading.Tasks;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.Storage.Interfaces;
using Codinex.Storage.Models;
using Codinex.Storage.Services;

namespace Codinex.Storage.Managers
{
    [AutoDiRegister(Modules.Storage, RegistrationOrder.Foundation)]
    public class SettingsManager(IStorageService storage)
    {
        private CodinexSettings _currentSettings = new(); // Default values

        // Get current settings
        public CodinexSettings Settings => _currentSettings;

        /// <summary>
        /// Loads settings from disk, or creates defaults if not found.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (await storage.ExistsAsync(StoragePaths.Settings))
            {
                var loaded = await storage.LoadAsync<CodinexSettings>(StoragePaths.Settings);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                }
            }
            else
            {
                // First run: save the default settings
                await SaveSettingsAsync(_currentSettings);
            }
        }

        /// <summary>
        /// Updates and persists settings.
        /// </summary>
        public async Task SaveSettingsAsync(CodinexSettings newSettings)
        {
            _currentSettings = newSettings;
            await storage.SaveAsync(StoragePaths.Settings, _currentSettings);

            // Note: Here we can notify the WebView to update its UI
        }
    }
}