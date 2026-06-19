/**
 * Lightweight global state manager for Chat UI.
 * Implements controlled mutations and subscriptions.
 */

const _state = {
    provider: null,
    selectedModels: null,
    currentModel: null,
    messages: [],
    isLoading: false,
    chatList: [],
    currentChat: null,
};

const listeners = [];

/**
 * Returns a frozen copy of state (read-only).
 */
export function getState() {
    return Object.freeze({ ..._state });
}

/**
 * Subscribe to state changes.
 */
export function subscribe(listener) {
    if (typeof listener !== 'function') return;

    listeners.push(listener);

    return () => {
        const index = listeners.indexOf(listener);
        if (index > -1) listeners.splice(index, 1);
    };
}

/**
 * Internal state update
 */
function updateState(partialState) {
    Object.assign(_state, partialState);

    listeners.forEach(listener => listener(getState()));
}

/**
 * Set active provider.
 * Automatically resets model when provider changes.
 */
export function setProvider(provider) {
    if (!provider) {
        throw new Error('Provider cannot be null or empty.');
    }

    updateState({
        provider: provider,
        selectedModels: _.filter(provider.models, { isSelected: true }),
        currentModel: _.filter(provider.models, { isCurrent: true })[0] || null
    });
}
/**
 * Set active model.
 * Automatically resets model when provider changes.
 */
export function setCurrentModel(model) {
    if (!model) {
        throw new Error('Model cannot be null or empty.');
    }

    updateState({
        currentModel: model
    });
}
/**
 * Set active provider.
 * Automatically resets model when provider changes.
 */
export function setChatList(chatList) {
    if (!Array.isArray(chatList)) {
        throw new Error('Chat list histories must be an array.');
    }

    updateState({
        chatList: chatList,
    });
}
/**
 * Set active model.
 * Automatically resets model when provider changes.
 */
export function setCurrentChat(chat) {
    if (!chat) {
        throw new Error('Chat cannot be null or empty.');
    }

    updateState({
        currentChat: chat   
    });
}
/**
 * Set loading state.
 */
export function setLoading(isLoading) {
    updateState({ isLoading: !!isLoading });
}

/**
 * Add message to history.
 */
export function addMessage(message) {
    if (!message) return;

    updateState({
        messages: [..._state.messages, message]
    });
}

/**
 * Clear chat history.
 */
export function clearMessages() {
    updateState({ messages: [] });
}
