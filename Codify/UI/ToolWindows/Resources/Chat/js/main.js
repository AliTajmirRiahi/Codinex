/**
 * main.js
 * The central entry point for the WebView UI.
 * Responsible for bootstrapping the entire frontend.
 */

import { webViewTransport } from '../../Shared/bridge/webViewTransport.js';
import { createMessageDispatcher } from '../../Shared/bridge/messageDispatcher.js';
import { initChatController } from './controllers/chatController.js';

// Register Custom Elements
import '../../Shared/components/CodifyIcon.js';
import '../../Shared/components/CodifyImage.js';

document.addEventListener('DOMContentLoaded', () => {
    console.log("Codify UI bootstrapping...");

    /**
     * Initialize Controllers
     */
    const chatController = initChatController(webViewTransport);

    /**
     * Setup message dispatcher
     * Routes incoming messages from .NET to handlers
     */
    const dispatcher = createMessageDispatcher({
        onInitData: (data) => {
            console.log("Init data received:", data);
            // chatController.loadProviders(data.providers);
        },

        onAIResponse: (payload) => {
            chatController.handleAIResponse(payload);
        },

        onError: (error) => {
            console.error("Bridge error:", error);
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
    webViewTransport.send('READY', {
        timestamp: Date.now()
    });

});
