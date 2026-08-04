import { manageModelsView } from '../views/manageModelsView.js';
import { EVENTS, CUSTOME_EVENTS } from '../constants/events.js';
/**
 * Orchestrates settings changes.
 */
export const initManageModelsController = (transport) => {

    // Open Modal logic
    document.getElementById('manage-models-action')?.addEventListener('click', () => {
        //Ask for close other dropdowns
        window.dispatchEvent(new CustomEvent(CUSTOME_EVENTS.CLOSE_ALL_DROPDOWNS));

        manageModelsView.show();
    });

    // Close Modal logic (assuming you have a close button or overlay)
    document.getElementById('close-settings')?.addEventListener('click', () => {
        manageModelsView.hide();
    });

    return {
        updateUI(settings) {
            manageModelsView.initEventHandlers(
                (updatedSettings) => {
                    this.sendUpdatedSettings(updatedSettings);
                },
                (payload) => {
                    this.refreshProviderModels(payload);
                }); // Ensure event handlers are set up
            manageModelsView.renderProviders(settings.availableProviders, (settings.current != null ? settings.current.id : -1));

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
