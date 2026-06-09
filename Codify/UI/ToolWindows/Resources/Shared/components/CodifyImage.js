/**
 * CodifyImage.js
 * Custom element to render images from extension resources.
 * Usage: <codify-image name="avatar.png"></codify-image>
 */

class CodifyImage extends HTMLElement {
    static get observedAttributes() {
        return ['name'];
    }

    connectedCallback() {
        this.render();
    }

    attributeChangedCallback() {
        this.render();
    }

    render() {
        const name = this.getAttribute('name');
        if (!name) return;

        // Note: Images are usually rendered via <img> tag or background
        // We use the same custom protocol as icons
        const url = `http://codify.resources/Icons/${name}`;

        this.innerHTML = `<img src="${url}" alt="${name}" style="max-width: 100%; height: auto;" />`;

        // Handle loading errors
        const img = this.querySelector('img');
        img.onerror = () => {
            console.error(`[CodifyImage] Failed to load image: ${url}`);
            this.innerHTML = `<span class="error-placeholder">Image not found</span>`;
        };
    }
}

if (!customElements.get('codify-image')) {
    customElements.define('codify-image', CodifyImage);
}
