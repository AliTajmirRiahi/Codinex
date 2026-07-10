import { EVENTS } from '../constants/events.js';
import { setState } from '../state/appState.js';

/**
 * Handles AI provider logic and selection.
 */
export const initProviderController = (transport) => {
    return {
        changeProvider(providerId) {
            setState({ provider: providerId });

            // Notify .NET to change the backend service
            transport.send(EVENTS.SELECT_PROVIDER, { providerId });
        }
    };
};
