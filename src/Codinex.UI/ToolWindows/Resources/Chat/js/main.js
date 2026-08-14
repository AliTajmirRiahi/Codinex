/**
 * main.js
 * The central entry point for the WebView UI.
 * Responsible for bootstrapping the entire frontend.
 */
import { getState, subscribe, setLoading, setInputLoading, setProvider, setCurrentModel, setChatList, setCurrentChat, setGroupList, setCurrentGroup, setComposerController, setActiveDocument, setSettings, setChatBlocked, setAwaitingClarification } from '../js/state/appState.js';
import { $, togglePanelHidden } from './utils/dom.js';
import { applyComposerDirection } from './utils/languageDirection.js';
import { webViewTransport } from '../../Shared/bridge/webViewTransport.js';
import { createMessageDispatcher } from '../../Shared/bridge/messageDispatcher.js';
import { initChatController } from './controllers/chatController.js';
import { AskUserQuestionView } from './views/askUserQuestionView.js';
import { initManageModelsController } from './controllers/manageModelsController.js';
import { initAboutController } from './controllers/aboutController.js';
import { initSettingsController } from './controllers/settingsController.js';
import { EVENTS } from '../js/constants/events.js';
import { reportError } from '../../Shared/bridge/errorReporter.js'

// Register Custom Elements
import '../../Shared/components/codinex-icon.js';
import '../../Shared/components/codinex-image.js';
import { initContextMenu } from '../../Shared/components/contextMenu.js';

window.addEventListener('error', (event) => {
    debugger;
    if (event.target && (event.target.src || event.target.href))
        reportError(`Failed to load resource: ${event.target.src || event.target.href}`, 'Network/Resource Error');
    else
        reportError(event.error || event.message, "window");
}, true);

window.addEventListener('unhandledrejection', (event) => {
    debugger;
    reportError(event.reason, "promise");
});


document.addEventListener('DOMContentLoaded', () => {

    setLoading(true);

    initContextMenu();

    /**
     * Initialize Controllers
     */
    const chatController = initChatController(webViewTransport);

    const manageModelsController = initManageModelsController(webViewTransport);
    initAboutController();
    const settingsController = initSettingsController(webViewTransport);

    const askUserQuestionView = new AskUserQuestionView({
        container: $('#ask-user-question-panel'),
        onAnswer: ({ requestId, answers }) => {
            webViewTransport.send(EVENTS.ASK_USER_ANSWER, { requestId, answers });
            askUserQuestionView.hide();
            setAwaitingClarification(false);
        }
    });

    function getChatsPayload(payload) {
        const chats = payload?.chats || payload?.Chats;

        if (!chats) return null;

        return {
            chatList: chats.chatList || chats.ChatList,
            current: chats.current || chats.Current
        };
    }

    function applyChatsPayload(payload) {
        const chats = getChatsPayload(payload);

        if (!chats) return;

        if (chats.chatList) setChatList(chats.chatList);
        if (chats.current) setCurrentChat(chats.current);

        chatController.renderChatList();
    }

    /**
     * Setup message dispatcher
     * Routes incoming messages from .NET to handlers
     */
    const dispatcher = createMessageDispatcher({
        onInitData: (data) => {

            subscribe(() => {
                var state = getState();
                togglePanelHidden('#input-loading-screen', state.isInputLoading);
                togglePanelHidden('#chat-blocked-banner', state.isChatBlocked);
                // A body-level class (rather than toggling the shared 'disable' class directly)
                // avoids fighting with composerView.js's own draft-text-driven send-btn state.
                document.body.classList.toggle('chat-blocked', state.isChatBlocked);
                document.body.classList.toggle('awaiting-clarification', state.isAwaitingClarification);
            })

            const chatBlocked = data.chatBlocked ?? data.ChatBlocked ?? false;
            setChatBlocked(chatBlocked);

            if (data.providers != null && data.providers.current) {
                setProvider(data.providers.current);
                chatController.renderCurrentProvider();
            }

            if (data.groups != null && data.groups.groupList) {
                setGroupList(data.groups.groupList);
                setCurrentGroup(data.groups.current);
                chatController.renderProjectList();
            }

            if (data.chats != null && data.chats.chatList) {
                setChatList(data.chats.chatList);
                setCurrentChat(data.chats.current)
                chatController.renderChatList();
                chatController.navigateToChat();
            }

            if (data.settings) {
                setSettings(data.settings);
            }

            if (data.references) {
                setComposerController(data.references);
                setActiveDocument(data.activeDocument);
                chatController.setComposerReferences();
                chatController.handleActiveDocument();
            }

            manageModelsController.updateUI(data.providers);
            settingsController.updateUI(data.settings, data.providers, data.workspaceSettings);

            // Get references to the loading screen and the main chat UI
            const loadingScreen = $('#loading-screen');
            const mainChatWrapper = $('#main-chat-wrapper');

            // Hide the loading screen and show the main chat UI once data is loaded
            if (loadingScreen && mainChatWrapper) {
                togglePanelHidden('#loading-screen', false);
                togglePanelHidden('#main-chat-wrapper', true);
            }

            setLoading(false);
        },

        onChangeModelSettingApproved: (payload) => {
            togglePanelHidden('#models-loading-screen', false);

            const providers = payload.providers || payload.Providers;
            const currentProvider = providers?.current || providers?.Current;

            if (currentProvider)
                setProvider(currentProvider);

            if (providers)
                manageModelsController.updateUI(providers);

            manageModelsController.closeProviderSettings();
            chatController.renderCurrentProvider();
        },
        onChangeModelSettingRejected: (payload) => {
            const providers = payload.providers || payload.Providers;
            const message = payload.message || payload.Message || 'Settings could not be saved.';

            if (providers)
                manageModelsController.updateUI(providers);

            manageModelsController.showSettingsError(message);
        },
        onProviderModelsRefreshed: (payload) => {
            const providers = payload.providers || payload.Providers;

            manageModelsController.updateUI(providers, payload.SelectedProviderId || payload.selectedProviderId);

            setInputLoading(false);
        },
        onSettingsSaved: (payload) => {
            const settings = payload.settings || payload.Settings;

            setSettings(settings);
            settingsController.updateUI(settings);
            chatController.handleSettingsChanged();
            settingsController.closeSettingsModal();
        },
        onSolutionInstructionSaved: (payload) => {
            const workspaceSettings = payload.workspaceSettings || payload.WorkspaceSettings;

            settingsController.updateUI(null, null, workspaceSettings);
        },
        onSelectModel: (payload) => {
            const providers = payload.providers || payload.Providers;
            const currentProvider = providers?.current || providers?.Current;
            const activeModel = payload.activeModel || payload.ActiveModel;

            if (currentProvider) {
                setProvider(currentProvider);
            } else if (activeModel) {
                setCurrentModel(activeModel);
            }

            manageModelsController.updateUI(providers);
            chatController.renderCurrentProvider();
            setInputLoading(false);
        },
        onSelectChat: (payload) => {
            setCurrentChat(payload.chat);
            chatController.navigateToChat();
        },
        onSelectGroup: (payload) => {
            setGroupList(payload.groups.groupList);
            setCurrentGroup(payload.groups.current);
            setChatList(payload.chats.chatList);
            setCurrentChat(payload.chats.current);
            chatController.renderProjectList();
            chatController.renderChatList();
            chatController.navigateToProject();
        },

        onAIResponse: (payload, meta) => {
            chatController.handleAIResponse(payload, meta);

            if (meta && (meta.titleChanged || meta.TitleChanged)) {
                applyChatsPayload(meta);
            }
        },
        onHandleStreamChunk: (payload, meta) => {
            chatController.handleStreamChunk(payload, meta);
        },
        onThinkingStarted: () => {
            chatController.handleThinkingStarted();
        },
        onThinkingChunk: (payload) => {
            chatController.handleThinkingChunk(payload);
        },
        onThinkingCompleted: () => {
            chatController.handleThinkingCompleted();
        },
        onStatusChanged: (payload) => {
            chatController.handleStatusChanged(payload);
        },

        onChatTitleChanged: (payload) => {
            applyChatsPayload(payload);
        },
        onNewChat: (payload) => {
            setChatList(payload.chats.chatList);
            setCurrentChat(payload.chats.current);
            chatController.renderChatList();
            chatController.navigateToChat();
        },  
        onActiveDocumentChanged: (payload) => {
            setActiveDocument(payload);
            chatController.handleActiveDocument();
        },
        onInputLanguageChanged: (payload) => {
            const isRightToLeft = payload?.isRightToLeft ?? payload?.IsRightToLeft ?? false;
            applyComposerDirection(isRightToLeft);
        },
        onError: (error) => {
            chatController.handleAIError(error);
        },
        onChatBlocked: () => {
            setChatBlocked(true);
        },
        onChatUnblocked: () => {
            setChatBlocked(false);
        },
        onAskUserQuestion: (payload) => {
            setAwaitingClarification(true);
            askUserQuestionView.show(payload);
        },
        onRewindChatApproved: (payload) => {
            chatController.handleRewindChatApproved(payload);
        }
    });

    /**
     * Listen for messages coming from .NET
     */
    webViewTransport.onMessage((data) => {
        dispatcher(data);
    });

    /**
     * Clicking the "pending review" banner re-shows the Code Changes window —
     * otherwise a user who closed it without deciding has no way back to it.
     */
    const chatBlockedBanner = $('#chat-blocked-banner');
    if (chatBlockedBanner) {
        const reopenPendingReview = () => webViewTransport.send(EVENTS.REOPEN_CHANGESET_REVIEW, {});

        chatBlockedBanner.addEventListener('click', reopenPendingReview);
        chatBlockedBanner.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                reopenPendingReview();
            }
        });
    }

    /**
     * Notify .NET backend that the UI is ready
     */
    webViewTransport.send(EVENTS.READY, {
        timestamp: Date.now()
    });

});
