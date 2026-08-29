/**
 * ChatController
 * Handles user interaction and coordinates between
 * the view layer and the service layer.
 */
import { createStreamingMessage, chatView } from '../views/chatView.js';
import { extractQuoteFromText } from '../views/messageView.js';
import { chatListView } from '../views/chatListView.js';
import { projectListView } from '../views/projectListView.js';
import { aiService } from '../services/aiService.js';
import { getState, setLoading, setCurrentModel, setInputLoading, setCurrentChat, setSelectedReferences, setSelectedCommand, setDraftText } from '../state/appState.js';
import { EVENTS } from '../constants/events.js';
import { STATICS } from '../constants/statics.js';
import { reportError } from '../../../Shared/bridge/errorReporter.js';
import { ComposerController } from '../controllers/composerController.js';


let activeStreamMessage = null;
let accumulatedText = '';

function getErrorMessage(payload) {
    if (!payload) return STATICS.GENERIC_CHAT_ERROR;

    if (typeof payload === 'string') {
        const trimmedPayload = payload.trim();

        if (!trimmedPayload) return STATICS.GENERIC_CHAT_ERROR;

        if (trimmedPayload.startsWith('{') || trimmedPayload.startsWith('[')) {
            try {
                return getErrorMessage(JSON.parse(trimmedPayload));
            } catch {
                return trimmedPayload;
            }
        }

        return trimmedPayload;
    }

    if (typeof payload !== 'object') {
        return String(payload);
    }

    const directMessage = payload.displayMessage
        || payload.DisplayMessage
        || payload.message
        || payload.Message;

    if (directMessage) {
        return getErrorMessage(directMessage);
    }

    const nestedPayload = payload.payload || payload.Payload;

    if (nestedPayload) {
        return getErrorMessage(nestedPayload);
    }

    const providerError = payload.error || payload.Error;

    if (providerError) {
        return getErrorMessage(providerError);
    }

    const details = payload.details || payload.Details || payload.detail || payload.Detail;

    if (details) {
        return getErrorMessage(details);
    }

    return STATICS.GENERIC_CHAT_ERROR;
}

function isImageReference(reference, metadata) {
    const type = reference?.type || reference?.Type;
    const signature = metadata?.signature || metadata?.Signature;
    const body = metadata?.body || metadata?.Body;

    return type === 'Image'
        || signature?.startsWith('image/')
        || body?.startsWith('data:image/');
}

/**
 * Initialize chat controller
 * @param {Object} transport - Communication transport with VS extension host
 */
export function initChatController(transport) {

    chatView.initialize(handleSend, onModelSelected, handleCancel);

    const composerController = new ComposerController(chatView.composer);

    chatView.composer.setOnChange((ctx) => {
        composerController.handleInput(ctx);
    });

    chatView.composer.setOnContextClicked((ctx) => {
        composerController.handleContextClick(ctx);
    });

    document.addEventListener('composer:ref-click', (e) => {
        const reference = e.detail?.reference;
        const metadata = reference?.metadata || reference?.Metadata || {};
        const filePath = metadata.filePath || metadata.FilePath || reference?.value || reference?.Value;

        if (!filePath) return;

        const startLine = metadata.startLine ?? metadata.StartLine;
        const endLine = metadata.endLine ?? metadata.EndLine;

        if (isImageReference(reference, metadata)) {
            transport.send(EVENTS.OPEN_REFERENCE_FILE, {
                filePath,
                fileName: reference?.name || reference?.Name || filePath,
                isImage: true,
                mimeType: metadata.signature || metadata.Signature,
                body: metadata.body || metadata.Body,
                content: metadata.content || metadata.Content
            });
            return;
        }

        transport.send(EVENTS.OPEN_REFERENCE_FILE, { filePath, startLine, endLine });
    });

    document.addEventListener('composer:quote-message', (e) => {
        const text = e.detail?.text;

        if (!text) return;

        chatView.composer.setQuote(text);
    });

    document.addEventListener('chat:rewind-to-message', (e) => {
        const messageIndex = e.detail?.messageIndex;

        if (!Number.isInteger(messageIndex)) return;

        const state = getState();

        if (state.isLoading || state.isChatBlocked || state.isAwaitingClarification) return;

        transport.send(EVENTS.REWIND_CHAT, { messageIndex });
    });

    chatListView.initialize(onChatSelected, handleNewChat, handleDeleteChat, handleEditChat);
    projectListView.initialize(onProjectSelected, handleNewProject, handleDeleteProject, handleEditProject);

    function onModelSelected(model) {
        var appState = getState();
        const data = {
            providerId: appState.provider.id,
            modelId: model.id
        };
        setInputLoading(true);
        transport.send(EVENTS.SELECT_MODEL, data);
    }

    function onChatSelected(chat) {
        const data = {
            chatId: chat.id
        };
        transport.send(EVENTS.SELECT_CHAT, data);
    }

    function onProjectSelected(project) {
        const data = {
            groupId: project.id
        };
        transport.send(EVENTS.SELECT_GROUP, data);
    }

    /**
     * Main send handler
     */
    async function handleSend() {
        const state = getState();

        if (state.composer.draftText == '') return;

        // Prevent sending while AI is generating
        if (state.isLoading) return;

        // Prevent sending while a code-changes review is pending decision
        if (state.isChatBlocked) return;

        // Prevent sending while the assistant is waiting on an ask_user_question answer
        if (state.isAwaitingClarification) return;

        chatView.clearStatus();
        chatView.resetThinking();

        const quotedText = chatView.composer.getQuote();
        const quotedMessage = quotedText
            ? { text: quotedText, author: 'You', time: new Date().toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) }
            : null;

        // Show user message immediately
        chatView.appendMessage(state.composer.draftText, 'user', null, false, {
            context: { selectedReferences: state.composer.selectedReferences, quotedMessage }
        });

        // Set loading state
        setLoading(true);

        try {

            const draftWithQuote = quotedText
                ? `> Quoted message:\n${quotedText.split('\n').map(line => `> ${line}`).join('\n')}\n\n${state.composer.draftText}`
                : state.composer.draftText;

            var messageModel = {
                draftText: draftWithQuote,
                selectedCommand: state.composer.selectedCommand,
                selectedAgent: state.composer.selectedAgent,
                selectedReferences: state.composer.selectedReferences,
            }
            composerController.resetComposer();
            chatView.composer.clearQuote();

            await aiService.sendMessage(messageModel, transport);

        } catch (error) {
            setLoading(false);

            reportError(error, "chatController");

            chatView.appendErrorMessage(STATICS.GENERIC_CHAT_ERROR);
        }
    }

    function handleCancel() {
        const state = getState();

        if (!state.isLoading) return;

        chatView.setStatus('Stopping...');
        transport.send(EVENTS.CANCEL_GENERATION);
    }

    async function handleNewChat() {
        chatView.clearMessages();
        transport.send(EVENTS.NEW_CHAT);
    }

    async function handleDeleteChat() {
        chatView.clearMessages();
        transport.send(EVENTS.DELETE_CHAT);
    }

    async function handleEditChat(chat) {
        const state = getState();
        const currentChat = state.currentChat;

        if (!chat || !chat.title || !currentChat || currentChat.isNewChat) return;

        const chatId = chat.id || currentChat.id;
        if (!chatId) return;

        transport.send(EVENTS.UPDATE_CHAT, {
            chatId: chatId,
            title: chat.title.substring(0, 25)
        });
    }

    async function handleNewProject(project) {
        if (!project || !project.name) return;

        chatView.clearMessages();
        transport.send(EVENTS.NEW_GROUP, {
            name: project.name,
            description: project.description || ''
        });
    }

    async function handleDeleteProject() {
        const state = getState();
        const currentGroup = state.currentGroup;

        if (!currentGroup || currentGroup.isDefault) return;

        chatView.clearMessages();
        transport.send(EVENTS.DELETE_GROUP, {
            groupId: currentGroup.id
        });
    }

    async function handleEditProject(project) {
        const state = getState();
        const currentGroup = state.currentGroup;
        if (!project || !project.name) return;
        const groupId = project.id || (currentGroup && currentGroup.id);
        if (!groupId) return;
        transport.send(EVENTS.UPDATE_GROUP, {
            groupId: groupId,
            name: project.name,
            description: project.description || ''
        });
    }
    /**
     * Returns public methods to allow external interaction with the controller
     */
    return {
        /**
         * Updates the active model from outside (e.g., when loading saved settings)
         */
        setActiveModel: (modelId) => {
        },

        /**
         * Clears the chat UI if needed
         */
        clearChat: () => {
            chatView.clearMessages();
        },

        renderCurrentProvider: () => {
            const appState = getState();

            const currentModelId = appState.currentModel
                ? appState.currentModel.id
                : null;

            chatView.renderModelMenu(
                appState.selectedModels,
                currentModelId
            );
        },

        renderChatList: () => {
            const appState = getState();

            const currentChatId = appState.currentChat
                ? appState.currentChat.id
                : null;

            chatListView.renderChatListMenu(
                appState.chatList,
                currentChatId
            );
        },

        renderProjectList: () => {
            const appState = getState();

            const currentGroupId = appState.currentGroup
                ? appState.currentGroup.id
                : null;

            projectListView.renderProjectListMenu(
                appState.groupList,
                currentGroupId
            );
        },

        setComposerReferences: () => {
            composerController.setRefrences();
        },

        handleAIResponse: (payload, meta) => {
            setLoading(false);
            chatView.clearStatus();
            chatView.completeThinking();

            const wasCancelled = !!(meta && (meta.cancelled || meta.Cancelled));

            if (wasCancelled) {
                if (activeStreamMessage) {
                    const finalText = payload || accumulatedText;

                    chatView.finalizeMessage(activeStreamMessage, finalText);

                    activeStreamMessage = null;
                    accumulatedText = '';
                }

                chatView.setStatus(STATICS.RESPONSE_STOPPED_BY_USER);
                return;
            }

            // If streaming was active → finalize it
            if (activeStreamMessage) {

                // Use full accumulated text OR payload (depending on backend behavior)
                const finalText = payload || accumulatedText;

                chatView.finalizeMessage(activeStreamMessage, finalText);

                activeStreamMessage = null;
                accumulatedText = '';

                return;
            }

            if (!payload) return;

            // Non-stream fallback
            chatView.appendMessage(payload, 'assistant', null, false, meta);
        },
        handleStatusChanged: (payload) => {
            chatView.setStatus(payload);
        },
        handleStreamChunk: (payload, meta) => {
            // Stop loading spinner only on first chunk
            if (!activeStreamMessage) {
                activeStreamMessage = createStreamingMessage(meta);
                accumulatedText = '';
            }

            accumulatedText += payload;

            chatView.updateMessage(activeStreamMessage, payload);
        },
        handleThinkingStarted: () => {
            chatView.showThinking();
        },
        handleThinkingChunk: (payload) => {
            chatView.appendThinking(payload);
        },
        handleThinkingCompleted: () => {
            chatView.completeThinking();
        },
        handleAIError: (payload) => {
            // Show AI errors through the dedicated error UI instead of rendering
            // structured provider payloads as chat messages.
            setLoading(false);
            chatView.clearStatus();
            chatView.completeThinking();

            if (activeStreamMessage && activeStreamMessage.parentElement) {
                activeStreamMessage.parentElement.remove();
            }

            activeStreamMessage = null;
            accumulatedText = '';

            chatView.appendErrorMessage(getErrorMessage(payload));
        },
        navigateToChat: () => {
            chatView.clearMessages();
            chatListView.setCurrentChatName();
            var appState = getState();
            chatView.renderMessages(appState.currentChat.messages);
        },
        navigateToProject: () => {
            chatView.clearMessages();
            projectListView.setCurrentProjectName();
            chatListView.setCurrentChatName();
            var appState = getState();
            chatView.renderMessages(appState.currentChat.messages);
        },
        handleChatTitleChanged: () => {
            chatListView.setCurrentChatName();
        },
        handleRewindChatApproved: (payload) => {
            const chat = payload?.chat || payload?.Chat;
            const rewindText = payload?.rewindText ?? payload?.RewindText ?? '';
            const rewindReferences = payload?.rewindReferences || payload?.RewindReferences || [];

            if (chat) {
                setCurrentChat(chat);
                chatView.clearMessages();
                chatView.renderMessages(chat.messages || chat.Messages);
            }

            composerController.resetComposer();
            chatView.composer.clearQuote();

            const extracted = extractQuoteFromText(rewindText);

            chatView.composer.setText(extracted.text);

            if (extracted.quote) {
                chatView.composer.setQuote(extracted.quote);
            }

            setSelectedReferences(rewindReferences);
            chatView.composer.updateReferenceChips(rewindReferences);

            if (typeof composerController.handleInput === 'function') {
                composerController.handleInput({ trigger: null });
            }

            chatView.composer.input.focus();
        },
        handleActiveDocument: () => {
            const state = getState();
            const activeDocument = state.activeDocument;
            const autoAddActiveDocumentToMessage = !!(
                state.settings?.autoAddActiveDocumentToMessage ||
                state.settings?.AutoAddActiveDocumentToMessage);

            if (autoAddActiveDocumentToMessage && activeDocument) {
                composerController.handleSpecialDocument('references', activeDocument, 'Active Document');
            }
        },
        handleSettingsChanged: () => {
            const state = getState();
            const autoAddActiveDocumentToMessage = !!(
                state.settings?.autoAddActiveDocumentToMessage ||
                state.settings?.AutoAddActiveDocumentToMessage);

            if (autoAddActiveDocumentToMessage) {
                const activeDocument = state.activeDocument;

                if (activeDocument) {
                    composerController.handleSpecialDocument('references', activeDocument, 'Active Document');
                }

                return;
            }

            composerController.removeActiveDocumentReference();
        },
        addSelectedCodeReference: (payload) => {
            if (!payload) return;

            composerController.handleSpecialDocument('references', payload, payload.name || payload.Name);
        },
        runCommandOnSelection: (payload) => {
            if (!payload) return;

            const selection = payload.selection || payload.Selection;
            const commandName = payload.commandName || payload.CommandName;

            if (!selection || !commandName) return;

            composerController.handleSpecialDocument('references', selection, selection.name || selection.Name);

            const command = composerController.data.commands.find(c => c.name === commandName);

            if (command) {
                // insertChip() requires a typed "/" trigger to replace, which doesn't exist here.
                setSelectedCommand(command);
                setDraftText(command.name);
            }

            // Mirrors ComposerView.send(): clear the input DOM, then go through
            // handleSendMessage() so the chat-welcome screen gets hidden like a normal send.
            chatView.composer.clear();
            chatView.handleSendMessage();
        }
    };
}
