import { manageModelsView } from '../views/manageModelsView.js';
import { EVENTS, CUSTOME_EVENTS } from '../constants/events.js';
/**
 * Orchestrates settings changes.
 */
export const initManageModelsController = (transport) => {
    let latestSettings = null;

    function getAvailableProviders(settings) {
        return settings?.availableProviders || settings?.AvailableProviders || [];
    }

    function getCurrentProviderId(settings) {
        const current = settings?.current || settings?.Current;

        return current != null ? (current.id || current.Id) : -1;
    }

    // Open Modal logic
    document.getElementById('manage-models-action')?.addEventListener('click', () => {
        //Ask for close other dropdowns
        window.dispatchEvent(new CustomEvent(CUSTOME_EVENTS.CLOSE_ALL_DROPDOWNS));

        if (latestSettings) {
            manageModelsView.renderProviders(
                getAvailableProviders(latestSettings),
                getCurrentProviderId(latestSettings));
        }

        manageModelsView.show();
    });

    // Close Modal logic (assuming you have a close button or overlay)
    document.getElementById('close-settings')?.addEventListener('click', () => {
        manageModelsView.hide();
    });

    return {
        updateUI(settings) {
            if (!settings) return;

            latestSettings = settings;

            manageModelsView.initEventHandlers(
                (updatedSettings) => {
                    this.sendUpdatedSettings(updatedSettings);
                },
                (payload) => {
                    this.refreshProviderModels(payload);
                }); // Ensure event handlers are set up
            manageModelsView.renderProviders(getAvailableProviders(settings), getCurrentProviderId(settings));

            // Ensure loading overlay is hidden after any UI update (e.g., after refresh completes)
            manageModelsView.setLoading(false);
        },
        sendUpdatedSettings(updatedSettings) {
            transport.send(EVENTS.UPDATE_SETTINGS, updatedSettings);
        },
        refreshProviderModels(payload) {
            transport.send(EVENTS.REFRESH_PROVIDER_MODELS, payload);
        },
        closeProviderSettings() {
            manageModelsView.hide();
        },
        showSettingsError(message) {
            manageModelsView.showError(message);
        },
        // We can expose show/hide if other controllers need to trigger it
        showSettings: () => manageModelsView.show()
    };
};
