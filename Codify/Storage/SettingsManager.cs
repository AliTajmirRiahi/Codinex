using System.Threading.Tasks;
using Codify.Storage.Models;

namespace Codify.Storage
{
    public class SettingsManager
    {
        private readonly IStorageService _storage;
        private CodifySettings _currentSettings;

        public SettingsManager(IStorageService storage)
        {
            _storage = storage;
            _currentSettings = new CodifySettings(); // Default values
        }

        // Get current settings
        public CodifySettings Settings => _currentSettings;

        /// <summary>
        /// Loads settings from disk, or creates defaults if not found.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (await _storage.ExistsAsync(StoragePaths.Settings))
            {
                var loaded = await _storage.LoadAsync<CodifySettings>(StoragePaths.Settings);
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
            await _storage.SaveAsync(StoragePaths.Settings, _currentSettings);

            // Note: Here we can notify the WebView to update its UI
        }
    }
}