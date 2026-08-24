import { addProviderView } from '../views/addProviderView.js';
import { EVENTS, CUSTOME_EVENTS } from '../constants/events.js';

/**
 * Orchestrates adding a user-defined custom provider.
 */
export const initAddProviderController = (transport) => {
    addProviderView.initEventHandlers((data) => {
        transport.send(EVENTS.ADD_CUSTOM_PROVIDER, data);
    });

    document.getElementById('add-custom-provider-btn')?.addEventListener('click', () => {
        // Ask for close other dropdowns
        window.dispatchEvent(new CustomEvent(CUSTOME_EVENTS.CLOSE_ALL_DROPDOWNS));

        addProviderView.show();
    });

    document.getElementById('close-add-provider-modal')?.addEventListener('click', () => {
        addProviderView.hide();
    });

    document.getElementById('cancel-add-provider-btn')?.addEventListener('click', () => {
        addProviderView.hide();
    });

    return {
        handleProviderAdded() {
            addProviderView.hide();
        },
        handleProviderAddRejected(message) {
            addProviderView.showError(message);
        },
    };
};
