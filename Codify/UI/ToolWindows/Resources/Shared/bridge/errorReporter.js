import { webViewTransport } from '../../Shared/bridge/webViewTransport.js';
import { EVENTS } from '../../Chat/js/constants/events.js'

export function reportError(error, source = "ui") {

    const payload = {
        type: "error",
        source: source,
        message: error?.message || "Unknown error",
        stack: error?.stack || null
    };

    console.error("[UI ERROR]", payload);

    webViewTransport.send(EVENTS.UI_ERROR, payload);
}
