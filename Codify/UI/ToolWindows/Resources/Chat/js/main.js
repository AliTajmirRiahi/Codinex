/**
 * main.js
 * The central entry point for the WebView UI.
 * Responsible for bootstrapping the entire frontend.
 */
import { getState, setLoading, setProvider, setChatList, setCurrentChat, setComposerController } from '../js/state/appState.js';
import { $, togglePanelHidden } from './utils/dom.js';
import { webViewTransport } from '../../Shared/bridge/webViewTransport.js';
import { createMessageDispatcher } from '../../Shared/bridge/messageDispatcher.js';
import { initChatController } from './controllers/chatController.js';
import { initManageModelsController } from './controllers/manageModelsController.js';
import { EVENTS } from '../js/constants/events.js';
import { reportError } from '../../Shared/bridge/errorReporter.js'

// Register Custom Elements
import '../../Shared/components/codify-icon.js';
import '../../Shared/components/codify-image.js';

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
    /**
     * Initialize Controllers
     */
    const chatController = initChatController(webViewTransport);

    const manageModelsController = initManageModelsController(webViewTransport);

    /**
     * Setup message dispatcher
     * Routes incoming messages from .NET to handlers
     */
    const dispatcher = createMessageDispatcher({
        onInitData: (data) => {

            if (data.providers != null && data.providers.current) {
                setProvider(data.providers.current);
                chatController.renderCurrentProvider();
            }

            if (data.chats != null && data.chats.chatList) {
                setChatList(data.chats.chatList);
                setCurrentChat(data.chats.current)
                chatController.renderChatList();
            }

            if (data.references) {
                setComposerController(data.references);
                chatController.setComposerReferences();
            }

            manageModelsController.updateUI(data.providers);

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

        onSelectProvider: (payload) => {
            if (payload.provider)
                setProvider({ provider: payload.provider });

            manageModelsController.closeProviderSettings();
        },

        onSelectChat: (payload) => {
            setCurrentChat(payload.chat);
            chatController.navigateToChat();
        },

        onAIResponse: (payload) => {
            chatController.handleAIResponse(payload);
        },
        onHandleStreamChunk: (payload) => {
            chatController.handleStreamChunk(payload);
        },

        onChatTitleChanged: (payload) => {
            setChatList(payload.chats.chatList);
            setCurrentChat(payload.chats.current);
            chatController.renderChatList();
        },
        onNewChat: (payload) => {
            setChatList(payload.chats.chatList);
            setCurrentChat(payload.chats.current);
            chatController.renderChatList();
            chatController.navigateToChat();
        },  
        onError: (error) => {
            chatController.handleAIError(error);
        }
    });

    /**
     * Listen for messages coming from .NET
     */
    webViewTransport.onMessage((data) => {
        dispatcher(data);
    });

    /**
     * Notify .NET backend that the UI is ready
     */
    webViewTransport.send(EVENTS.READY, {
        timestamp: Date.now()
    });

});
