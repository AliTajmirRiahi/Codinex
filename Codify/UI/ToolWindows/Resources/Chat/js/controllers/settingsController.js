import { settingsView } from '../views/settingsView.js';
import { EVENTS } from '../../js/constants/events.js';
/**
 * Orchestrates settings changes.
 */
export const initSettingsController = (transport) => {

    //// Listen for changes in the UI
    //document.getElementById('provider-select')?.addEventListener('change', (e) => {
    //    const providerId = e.target.value;
    //    transport.send('UPDATE_SETTINGS', { provider: providerId });
    //});

    // Open Modal logic
    document.getElementById('settings-btn')?.addEventListener('click', () => {
        settingsView.show();
    });

    // Close Modal logic (assuming you have a close button or overlay)
    document.getElementById('close-settings')?.addEventListener('click', () => {
        settingsView.hide();
    });

    return {
        updateUI(settings) {
            settingsView.initEventHandlers((updatedSettings) => {
                this.sendUpdatedSettings(updatedSettings);
            }); // Ensure event handlers are set up
            settingsView.renderProviders(settings.availableProviders, (settings.current != null ? settings.current.id : -1));
        },
        sendUpdatedSettings(updatedSettings) {
            transport.send(EVENTS.UPDATE_SETTINGS, updatedSettings);
        },
        closeProviderSettings() {
            settingsView.hide();
        },
        // We can expose show/hide if other controllers need to trigger it
        showSettings: () => settingsView.show()
    };
};
