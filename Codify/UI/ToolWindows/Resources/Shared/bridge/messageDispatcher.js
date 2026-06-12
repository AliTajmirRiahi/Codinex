/**
 * messageDispatcher.js
 * Routes incoming messages from the bridge to specific application logic.
 */
import { EVENTS } from '../../Chat/js/constants/events.js'
export function createMessageDispatcher(handlers) {
    /**
     * The dispatcher function.
     * @param {Object} message - The raw message from .NET (type and payload).
     */
    return function dispatch(message) {
        if (!message || !message.type) {
            console.error("[Dispatcher] Invalid message format received", message);
            return;
        }

        const { type, payload } = message;

        switch (type) {
            case EVENTS.INIT_DATA:
                if (handlers.onInitData) handlers.onInitData(payload);
                break;

            case EVENTS.SELECT_PROVIDER:
                if (handlers.onSelectProvider) handlers.onSelectProvider(payload);
                break;

            case EVENTS.AI_RESPONSE:
                if (handlers.onAIResponse) handlers.onAIResponse(payload);
                break;

            case EVENTS.ERROR:
                // Fixed the typo from the original bridge.js (Ppayload -> payload)
                if (handlers.onError) handlers.onError(payload.Message || payload);
                break;

            default:
                console.warn(`[Dispatcher] No handler registered for message type: ${type}`);
        }
    };
}
