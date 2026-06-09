import { settingsView } from '../views/settingsView.js';

/**
 * Orchestrates settings changes.
 */
export const initSettingsController = (transport) => {
    // Listen for changes in the UI
    document.getElementById('provider-select')?.addEventListener('change', (e) => {
        const providerId = e.target.value;
        transport.send('UPDATE_SETTINGS', { provider: providerId });
    });

    return {
        updateUI(settings) {
            settingsView.renderProviders(settings.availableProviders, settings.current);
        }
    };
};
