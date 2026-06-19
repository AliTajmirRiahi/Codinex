/**
 * main.js
 * The central entry point for the WebView UI.
 * Responsible for bootstrapping the entire frontend.
 */
import { setLoading, setProvider } from '../js/state/appState.js';
import { $, togglePanelHidden} from './utils/dom.js';
import { webViewTransport } from '../../Shared/bridge/webViewTransport.js';
import { createMessageDispatcher } from '../../Shared/bridge/messageDispatcher.js';
import { initChatController } from './controllers/chatController.js';
import { initManageModelsController } from './controllers/manageModelsController.js';
import { EVENTS } from '../js/constants/events.js'

// Register Custom Elements
import '../../Shared/components/codify-icon.js';
import '../../Shared/components/codify-image.js';

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

            if (data.providers.current) {
                setProvider(data.providers.current);
                chatController.renderCurrentProvider();
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

        onAIResponse: (payload) => {
            chatController.handleAIResponse(payload);
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
