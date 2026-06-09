/**
 * events.js
 * Centralized event names for communication between JS and .NET.
 */

export const EVENTS = {
    // Outgoing (To C#)
    SEND_MESSAGE: 'SEND_MESSAGE',
    INIT_STATE: 'INIT_STATE',
    SELECT_PROVIDER: 'SELECT_PROVIDER',
    CANCEL_GENERATION: 'CANCEL_GENERATION',

    // Incoming (From C#)
    INIT_DATA: 'INIT_DATA',
    AI_RESPONSE: 'AI_RESPONSE',
    STREAM_CHUNK: 'STREAM_CHUNK',
    ERROR: 'ERROR',
    SET_LOADING: 'SET_LOADING'
};
