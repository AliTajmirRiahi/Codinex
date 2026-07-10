using System.Threading.Tasks;
using Codify.Storage.Models;

namespace Codify.Storage
{
    public class SettingsManager(IStorageService storage)
    {
        private CodifySettings _currentSettings = new(); // Default values

        // Get current settings
        public CodifySettings Settings => _currentSettings;

        /// <summary>
        /// Loads settings from disk, or creates defaults if not found.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (await storage.ExistsAsync(StoragePaths.Settings))
            {
                var loaded = await storage.LoadAsync<CodifySettings>(StoragePaths.Settings);
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
        public async Task SaveSettingsAsync(CodifySettings newSettings)
        {
            _currentSettings = newSettings;
            await storage.SaveAsync(StoragePaths.Settings, _currentSettings);

            // Note: Here we can notify the WebView to update its UI
        }
    }
}