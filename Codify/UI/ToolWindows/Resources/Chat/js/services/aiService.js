/**
 * aiService.js
 * Bridges the UI logic with the .NET AI UseCases.
 */

import { EVENTS } from '../constants/events.js';

export const aiService = {
    /**
     * Sends a chat message to the backend.
     * @param {string} prompt - User input
     * @param {object} transport - The webViewTransport instance
     */
    sendMessage(prompt, transport) {
        transport.send(EVENTS.SEND_MESSAGE, {
            content: prompt,
            timestamp: new Date().toISOString()
        });
    },

    /**
     * Handles the stream chunks.
     * In a clean C# architecture, you usually send multiple 'AI_RESPONSE' 
     * events or a specific 'STREAM_CHUNK' event.
     */
    processStreamChunk(payload, state) {
        // Logic to append chunk to the current message state
        if (payload && payload.chunk) {
            state.appendToCurrentMessage(payload.chunk);
        }
    }
};
