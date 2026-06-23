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
            throw new Error("[Dispatcher] Invalid message format received from .NET");
        }

        const { type, payload } = message;

        switch (type) {
            case EVENTS.INIT_DATA:
                if (handlers.onInitData) handlers.onInitData(payload);
                break;

            case EVENTS.SELECT_PROVIDER:
                if (handlers.onSelectProvider) handlers.onSelectProvider(payload);
                break;

            case EVENTS.SELECT_CHAT_APPROVED:
                if (handlers.onSelectChat) handlers.onSelectChat(payload);
                break;

            case EVENTS.AI_RESPONSE:
                if (handlers.onAIResponse) handlers.onAIResponse(payload);
                break;

            case EVENTS.STREAM_CHUNK:
                if (handlers.onHandleStreamChunk) handlers.onHandleStreamChunk(payload);
                break;

            case EVENTS.CHAT_TITLE_CHANGED:
                if (handlers.onChatTitleChanged) handlers.onChatTitleChanged(payload);
                break;

            case EVENTS.NEW_CHAT:
                if (handlers.onNewChat) handlers.onNewChat(payload);
                break;

            case EVENTS.ERROR:
                // Fixed the typo from the original bridge.js (Ppayload -> payload)
                if (handlers.onError) handlers.onError(payload.Message || payload);
                break;

            default:
                throw new Error(`[Dispatcher] Unhandled message type: ${type}`);
        }
    };
}
