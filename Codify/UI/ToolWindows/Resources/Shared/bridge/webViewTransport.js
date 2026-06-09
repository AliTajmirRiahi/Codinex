/**
 * webViewTransport.js
 * Low-level transport layer for WebView2 communication.
 */

export const webViewTransport = {
    /**
     * Checks if the WebView2 environment is available.
     * @returns {boolean}
     */
    isAvailable() {
        return !!(window.chrome && window.chrome.webview);
    },

    /**
     * Sends a message to the .NET host.
     * @param {string} type - The event type/command.
     * @param {any} payload - The data to send.
     */
    send(type, payload) {
        if (!this.isAvailable()) {
            console.warn(`[Transport] WebView2 not available. Message dropped: ${type}`, payload);
            return;
        }
        window.chrome.webview.postMessage({ type, payload });
    },

    /**
     * Registers a listener for messages coming from the .NET host.
     * @param {Function} callback - Function to handle the incoming data.
     */
    onMessage(callback) {
        if (!this.isAvailable()) return;

        window.chrome.webview.addEventListener('message', (event) => {
            // event.data contains the object sent from .NET
            callback(event.data);
        });
    }
};
