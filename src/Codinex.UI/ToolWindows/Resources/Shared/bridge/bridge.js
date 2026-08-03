/**
 * bridge.js (Legacy Wrapper)
 * Re-exports new modules to maintain backward compatibility if needed.
 */

import { webViewTransport } from './webViewTransport.js';
import { createMessageDispatcher } from './messageDispatcher.js';

// Export everything together as a default object
export default {
    transport: webViewTransport,
    createDispatcher: createMessageDispatcher
};

// Also expose to window for any non-module scripts (if any)
window.CodifyBridge = {
    send: (t, p) => webViewTransport.send(t, p),
    isWebViewAvailable: () => webViewTransport.isAvailable()
};
