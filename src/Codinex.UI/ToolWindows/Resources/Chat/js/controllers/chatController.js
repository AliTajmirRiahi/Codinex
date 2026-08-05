/**
 * ChatController
 * Handles user interaction and coordinates between
 * the view layer and the service layer.
 */
import { createStreamingMessage, chatView } from '../views/chatView.js';
import { chatListView } from '../views/chatListView.js';
import { projectListView } from '../views/projectListView.js';
import { aiService } from '../services/aiService.js';
import { getState, setLoading, setCurrentModel, setInputLoading } from '../state/appState.js';
import { EVENTS } from '../constants/events.js';
import { STATICS } from '../constants/statics.js';
import { reportError } from '../../../Shared/bridge/errorReporter.js';
import { ComposerController } from '../controllers/composerController.js';


let activeStreamMessage = null;
let accumulatedText = '';

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

        chatView.clearStatus();

        // Show user message immediately
        chatView.appendMessage(state.composer.draftText, 'user');

        // Set loading state
        setLoading(true);

        try {

            var messageModel = {
                draftText: state.composer.draftText,
                selectedCommand: state.composer.selectedCommand,
                selectedAgent: state.composer.selectedAgent,
                selectedReferences: state.composer.selectedReferences,
            }
            composerController.resetComposer();

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
            chatView.appendMessage(payload, 'assistant');
        },
        handleStatusChanged: (payload) => {
            chatView.setStatus(payload);
        },
        handleStreamChunk: (payload) => {
            // Stop loading spinner only on first chunk
            if (!activeStreamMessage) {
                activeStreamMessage = createStreamingMessage();
                accumulatedText = '';
            }

            accumulatedText += payload;

            chatView.updateMessage(activeStreamMessage, payload);
        },
        handleAIError: (payload) => {
            // Show AI Error
            setLoading(false);
            chatView.clearStatus();
            chatView.appendErrorMessage(payload);
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
        }
    };
}
