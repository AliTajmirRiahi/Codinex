import { manageModelsView } from '../views/manageModelsView.js';
import { EVENTS } from '../constants/events.js';
/**
 * Orchestrates settings changes.
 */
export const initManageModelsController = (transport) => {

    //// Listen for changes in the UI
    //document.getElementById('provider-select')?.addEventListener('change', (e) => {
    //    const providerId = e.target.value;
    //    transport.send('UPDATE_SETTINGS', { provider: providerId });
    //});

    // Open Modal logic
    document.getElementById('manage-models-action')?.addEventListener('click', () => {
        //Ask for close other dropdowns
        window.dispatchEvent(new CustomEvent('ui:close-all-dropdowns'));

        manageModelsView.show();
    });

    // Close Modal logic (assuming you have a close button or overlay)
    document.getElementById('close-settings')?.addEventListener('click', () => {
        manageModelsView.hide();
    });

    return {
        updateUI(settings) {
            manageModelsView.initEventHandlers((updatedSettings) => {
                this.sendUpdatedSettings(updatedSettings);
            }); // Ensure event handlers are set up
            manageModelsView.renderProviders(settings.availableProviders, (settings.current != null ? settings.current.id : -1));
        },
        sendUpdatedSettings(updatedSettings) {
            transport.send(EVENTS.UPDATE_SETTINGS, updatedSettings);
        },
        closeProviderSettings() {
            manageModelsView.hide();
        },
        // We can expose show/hide if other controllers need to trigger it
        showSettings: () => manageModelsView.show()
    };
};
