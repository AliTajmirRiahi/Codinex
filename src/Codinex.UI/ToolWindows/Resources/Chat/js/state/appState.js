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
    isScreenLoading: true,
    isInputLoading : false,
    isChatBlocked: false,
    isAwaitingClarification: false,
    chatList: [],
    currentChat: null,
    groupList: [],
    currentGroup: null,
    composerReferences: [],
    activeDocument: null,
    settings: {},
    solutionDirectory: '',
    solutionName: '',

    composer: {
        draftText: "",
        activeTrigger: null,
        activeMenu: null,
        selectedCommand: null,
        selectedAgent: null,
        selectedReferences: [],
        cursorContext: null,
        filterText: ""
    }
};

const listeners = [];

/**
 * Returns a frozen copy of state (read-only).
 */
export function getState() {
    return Object.freeze({
        ..._state,
        composer: Object.freeze({
            ..._state.composer,
            selectedReferences: Object.freeze([..._state.composer.selectedReferences]),
        }),
        messages: Object.freeze([..._state.messages]),
        chatList: Object.freeze([..._state.chatList]),
        groupList: Object.freeze([..._state.groupList]),
        selectedModels: _state.selectedModels ? Object.freeze([..._state.selectedModels]) : null,
        settings: Object.freeze({ ..._state.settings }),
    });
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

    const modelId = model.id || model.Id;
    const selectedModels = _state.selectedModels
        ? _state.selectedModels.map(item => (item.id || item.Id) === modelId ? model : item)
        : _state.selectedModels;

    updateState({
        selectedModels: selectedModels,
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

    const chatId = chat.id || chat.Id;
    const chatList = _state.chatList.map(item => (item.id || item.Id) === chatId ? chat : item);

    updateState({
        currentChat: chat,
        chatList: chatList
    });
}

export function setGroupList(groupList) {
    if (!Array.isArray(groupList)) {
        throw new Error('Group list histories must be an array.');
    }

    updateState({
        groupList: groupList,
    });
}

export function setCurrentGroup(group) {
    if (!group) {
        throw new Error('Group cannot be null or empty.');
    }

    updateState({
        currentGroup: group
    });
}
/**
 * Set loading state.
 */
export function setLoading(isLoading) {
    updateState({ isLoading: !!isLoading });
}

/**
 * Set loading state.
 */
export function setScreenLoading(isScreenLoading) {
    updateState({ isScreenLoading: !!isScreenLoading });
}

/**
 * Set loading state.
 */
export function setInputLoading(isInputLoading) {
    updateState({ isInputLoading: !!isInputLoading });
}

/**
 * Set whether chat is locked because a changeset review is pending.
 */
export function setChatBlocked(isChatBlocked) {
    updateState({ isChatBlocked: !!isChatBlocked });
}
/**
 * Set whether chat is locked because the assistant is waiting on an
 * ask_user_question answer.
 */
export function setAwaitingClarification(isAwaitingClarification) {
    updateState({ isAwaitingClarification: !!isAwaitingClarification });
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

/**
 * Composer setters
 */
export function setDraftText(text) {
    if (typeof text !== 'string') {
        throw new Error('Draft text must be a string.');
    }

    const composer = { ..._state.composer, draftText: text };

    updateState({ composer });
}

export function setActiveTrigger(trigger) {
    // trigger can be null to clear
    const composer = { ..._state.composer, activeTrigger: trigger };

    updateState({ composer });
}

export function setActiveMenu(menu) {
    // menu can be null to clear
    const composer = { ..._state.composer, activeMenu: menu };

    updateState({ composer });
}

export function setSelectedCommand(command) {
    // command can be null to clear selection
    const composer = { ..._state.composer, selectedCommand: command };

    updateState({ composer });
}

export function setSelectedAgent(agent) {
    const composer = { ..._state.composer, selectedAgent: agent };

    updateState({ composer });
}

export function setSelectedReferences(references) {
    if (!Array.isArray(references)) {
        throw new Error('Selected references must be an array.');
    }

    const composer = { ..._state.composer, selectedReferences: references };

    updateState({ composer });
}

export function setCursorContext(context) {
    // context can be any value (object/string/null)
    const composer = { ..._state.composer, cursorContext: context };

    updateState({ composer });
}

export function resetComposer() {
    const composer = {
        draftText: "",
        activeTrigger: null,
        activeMenu: null,
        selectedCommand: null,
        selectedAgents: [],
        selectedReferences: [],
        cursorContext: null,
        filterText: ""
    };

    updateState({ composer });
}

export function setComposerController(refs) {
    if (!Array.isArray(refs)) {
        throw new Error('Composer references must be an array.');
    }

    updateState({
        composerReferences: refs
    });
}

/**
 * Adds a file reference to the composer's browsable list, or replaces the existing
 * entry for the same file path if one is already present. Used to keep the '@' file
 * picker in sync with add/edit events from the workspace file watcher.
 */
export function upsertComposerReference(item) {
    if (!item) return;

    const value = (item.value ?? item.Value ?? '').toLowerCase();
    const withoutExisting = _state.composerReferences.filter(
        r => (r.value ?? r.Value ?? '').toLowerCase() !== value
    );

    updateState({
        composerReferences: [...withoutExisting, item]
    });
}

/**
 * Removes the file reference matching the given file path from the composer's
 * browsable list. Used when the workspace file watcher reports a file deletion.
 */
export function removeComposerReference(filePath) {
    if (!filePath) return;

    const target = filePath.toLowerCase();

    updateState({
        composerReferences: _state.composerReferences.filter(
            r => (r.value ?? r.Value ?? '').toLowerCase() !== target
        )
    });
}

export function setActiveDocument(doc) {
    updateState({
        activeDocument: doc
    });
}

export function setSettings(settings) {
    updateState({
        settings: settings || {}
    });
}

export function setSolutionDirectory(solutionDirectory) {
    updateState({
        solutionDirectory: solutionDirectory || ''
    });
}

export function setSolutionName(solutionName) {
    updateState({
        solutionName: solutionName || ''
    });
}
