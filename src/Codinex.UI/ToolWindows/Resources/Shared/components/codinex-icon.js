/**
 * CodinexIcon.js
 * Custom element to render SVG icons from extension resources.
 * Usage: <codinex-icon name="send"></codinex-icon>
 *
 * A "name" that is a data URI (e.g. "data:image/svg+xml;base64,...") is treated as a
 * user-provided icon (custom AI provider logos) instead of a bundled resource name —
 * there is no file on disk to resolve through the http://codinex.resources reference
 * protocol, so the image is embedded directly wherever it's stored (provider.icon).
 */

class CodinexIcon extends HTMLElement {
    static get observedAttributes() {
        return ['name'];
    }

    async connectedCallback() {
        this.render();
    }

    attributeChangedCallback() {
        this.render();
    }

    async render() {
        const name = this.getAttribute('name');
        if (!name) return;

        if (name.startsWith('data:')) {
            this.renderDataUri(name);
            return;
        }

        try {
            // Using the custom protocol defined in the .NET side
            const url = `http://codinex.resources/Icons/${name}.svg`;
            const response = await fetch(url);

            if (response.ok) {
                const svgText = await response.text();
                this.innerHTML = svgText;
            } else {
                console.error(`[CodinexIcon] Failed to load icon: ${name}`);
            }
        } catch (err) {
            console.error(`[CodinexIcon] Error fetching icon: ${name}`, err);
        }
    }

    /**
     * Renders an embedded data-URI icon as an <img>, never by inlining its markup —
     * an <img> source never executes scripts or event handlers that might be embedded
     * in a user-picked SVG file, unlike setting innerHTML from raw SVG text would.
     */
    renderDataUri(dataUri) {
        this.innerHTML = '';

        const img = document.createElement('img');
        img.src = dataUri;
        img.alt = '';
        img.style.width = '100%';
        img.style.height = '100%';
        img.style.objectFit = 'contain';

        this.appendChild(img);
    }
}

// Register the component
if (!customElements.get('codinex-icon')) {
    customElements.define('codinex-icon', CodinexIcon);
}
