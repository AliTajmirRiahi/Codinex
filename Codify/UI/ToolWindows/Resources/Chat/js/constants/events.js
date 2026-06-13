/**
 * events.js
 * Centralized event names for communication between JS and .NET.
 */

export const EVENTS = {
    // Outgoing
    READY: "READY",
    SEND_MESSAGE: 'SEND_MESSAGE',
    INIT_STATE: 'INIT_STATE',
    SELECT_PROVIDER: 'SELECT_PROVIDER',
    SELECT_MODEL: 'SELECT_MODEL',
    CANCEL_GENERATION: 'CANCEL_GENERATION',
    UPDATE_SETTINGS: "UPDATE_SETTINGS",

    // Incoming
    INIT_DATA: 'INIT_DATA',
    AI_RESPONSE: 'AI_RESPONSE',
    STREAM_CHUNK: 'STREAM_CHUNK',
    ERROR: 'ERROR',
    SET_LOADING: 'SET_LOADING'
};


export const CUSTOME_EVENTS = {
   CLOSE_ALL_DROPDOWNS : "ui:close-all-dropdowns"
}