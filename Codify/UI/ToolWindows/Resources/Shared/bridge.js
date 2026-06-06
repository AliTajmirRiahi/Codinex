(function () {

    function isWebViewAvailable() {
        return !!(window.chrome && window.chrome.webview);
    }

    function send(type, payload) {
        if (!isWebViewAvailable()) {
            console.warn("WebView bridge unavailable", type, payload);
            return;
        }

        window.chrome.webview.postMessage({
            type: type,
            payload: payload
        });
    }

    function onMessage(handler) {
        if (!isWebViewAvailable()) return;

        window.chrome.webview.addEventListener("message", (event) => {
            handler(event.data);
        });
    }

    window.CodifyBridge = {
        send,
        onMessage,
        isWebViewAvailable
    };

})();

// Codify icon web component
// Loads SVG icons dynamically from embedded resources

class CodifyIcon extends HTMLElement {

    async connectedCallback() {

        const name = this.getAttribute("name");

        if (!name)
            return;

        const url = `http://codify.resources/Icons/${name}.svg`;

        try {

            const response = await fetch(url);
            const svg = await response.text();

            this.innerHTML = svg;

        } catch (err) {

            console.error("Icon load failed:", name, err);

        }
    }
}

customElements.define("codify-icon", CodifyIcon);

// Codify Image web component
// Loads Images dynamically from embedded resources

class CodifyImage extends HTMLElement {

    async connectedCallback() {

        const name = this.getAttribute("name");

        if (!name)
            return;

        const url = `http://codify.resources/Icons/${name}`;

        try {

            const response = await fetch(url);
            const img = await response.text();

            this.innerHTML = img;

        } catch (err) {

            console.error("Image load failed:", name, err);
        }
    }
}

customElements.define("codify-image", CodifyImage);

