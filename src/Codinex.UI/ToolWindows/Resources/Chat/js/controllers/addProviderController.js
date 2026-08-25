import { addProviderView } from '../views/addProviderView.js';
import { EVENTS, CUSTOME_EVENTS } from '../constants/events.js';

/**
 * Orchestrates adding and editing user-defined custom providers.
 */
export const initAddProviderController = (transport) => {
    addProviderView.initEventHandlers((data, editingProviderId) => {
        if (editingProviderId) {
            transport.send(EVENTS.EDIT_CUSTOM_PROVIDER, { ...data, providerId: editingProviderId });
        } else {
            transport.send(EVENTS.ADD_CUSTOM_PROVIDER, data);
        }
    });

    document.getElementById('add-custom-provider-btn')?.addEventListener('click', () => {
        // Ask for close other dropdowns
        window.dispatchEvent(new CustomEvent(CUSTOME_EVENTS.CLOSE_ALL_DROPDOWNS));

        addProviderView.show();
    });

    // Fired by the pencil button next to a user-added provider in the provider dropdown.
    window.addEventListener(CUSTOME_EVENTS.EDIT_CUSTOM_PROVIDER, (e) => {
        window.dispatchEvent(new CustomEvent(CUSTOME_EVENTS.CLOSE_ALL_DROPDOWNS));

        addProviderView.showForEdit(e.detail);
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
        handleProviderUpdated() {
            addProviderView.hide();
        },
        handleProviderUpdateRejected(message) {
            addProviderView.showError(message);
        },
    };
};
