/**
 * Global application state.
 * This acts as a lightweight state manager for the chat UI.
 */

const state = {

    // Current AI provider (openai, ollama, etc.)
    provider: null,

    // Current model
    model: null,

    // Chat messages history
    messages: [],

    // Indicates if AI is generating a response
    isLoading: false
};


/**
 * Returns current state (read-only usage recommended).
 */
export function getState() {
    return state;
}


/**
 * Update part of the state.
 * This keeps updates predictable and centralized.
 */
export function setState(partialState) {

    Object.assign(state, partialState);

}


/**
 * Add message to chat history
 */
export function addMessage(message) {

    state.messages.push(message);

}
